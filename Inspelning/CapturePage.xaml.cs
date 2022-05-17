using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Devices.Enumeration;
using Windows.Graphics.Display;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Inspelning.Recorder.CustomControls;
using Inspelning.Recorder.Services;
using Inspelning.Recorder.Utils;
using Unity;
using static Inspelning.Recorder.Utils.Constants;

namespace Inspelning.Recorder
{
    public sealed partial class CapturePage
    {
        private const int MaxDurationInMinutes = 120;
        private const int DefaultVideoWidth = 1280;
        private const int DefaultVideoHeight = 720;
        private readonly IDeviceService _deviceService;
        private readonly IDialogService _dialogService;
        private readonly DisplayRequest _displayRequest = new();
        private readonly IFileService _fileService;
        private StorageFolder _captureFolder;
        private bool _isInitialized;
        private bool _isPaused;
        private bool _isPreviewing;
        private bool _isRecording;
        private MediaCapture _mediaCapture;
        private int _secondsCount;
        private DispatcherTimer _timer;

        public CapturePage() : this(App.Container.Resolve<DialogService>(), App.Container.Resolve<DeviceService>(),
            App.Container.Resolve<FileService>())
        {
        }

        public CapturePage(IDialogService dialogService, IDeviceService deviceService,
            IFileService fileService)
        {
            _dialogService = dialogService;
            _deviceService = deviceService;
            _fileService = fileService;

            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Useful to know when to initialize/clean up the camera
            Application.Current.Suspending += CurrentSuspending;

            await InitializeCameraAsync();

            await _deviceService.PopulateDevicesUi(DeviceClass.VideoCapture, ComboBoxCameras);
            await _deviceService.PopulateDevicesUi(DeviceClass.AudioCapture, ComboBoxMicrophones);
        }

        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            try
            {
                await StopPreviewAsync();
                await CleanupCameraAsync();
            }
            catch (Exception ex)
            {
                _dialogService.DisplayDialog(ex.Message, ResourceRetriever.GetString("ButtonTextOk"));
                LogHelper.Error(ex.ToString());
                Debug.WriteLine(ex);
            }
        }

        private async void RecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
            {
                return;
            }

            try
            {
                if (_isRecording)
                {
                    await StopRecordingAsync();
                }
                else
                {
                    await StartRecordingAsync();
                }
            }
            catch (Exception ex)
            {
                _dialogService.DisplayDialog(ex.Message, ResourceRetriever.GetString("ButtonTextOk"));
                LogHelper.Error(ex.ToString());
                Debug.WriteLine(ex);
            }
            finally
            {
                // After starting or stopping video recording, update the UI to reflect the MediaCapture state
                UpdateCaptureControls();
            }
        }

        private async void PauseButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isPaused)
            {
                await _mediaCapture.ResumeRecordAsync();
                TextBlockRecording.Text = ResourceRetriever.GetString("CapturePageRecText");
                TextBlockPause.Visibility = Visibility.Collapsed;
                StartTimer();
                PreviewControl.Opacity = 1.0;
                _isPaused = false;
                _isRecording = true;
            }
            else
            {
                await _mediaCapture.PauseRecordAsync(MediaCapturePauseBehavior.RetainHardwareResources);
                TextBlockRecording.Text = ResourceRetriever.GetString("CapturePagePauseText");
                TextBlockPause.Visibility = Visibility.Visible;
                PauseTimer();
                PreviewControl.Opacity = 0.1;
                _isPaused = true;
                _isRecording = false;
            }
        }

        private async void DevicesSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_isPreviewing || !_isInitialized || _isRecording)
            {
                return;
            }

            await StopPreviewAsync();
            await CleanupCameraAsync();

            try
            {
                var selectedCameraItem = (ComboBoxItem)ComboBoxCameras?.SelectedItem;
                var selectedMicrophoneItem = (ComboBoxItem)ComboBoxMicrophones?.SelectedItem;
                var cameraDeviceId = selectedCameraItem?.Tag.ToString();
                var microphoneDeviceId = selectedMicrophoneItem?.Tag.ToString();

                await InitializeCameraAsync(cameraDeviceId, microphoneDeviceId);
            }
            catch (Exception ex)
            {
                _dialogService.DisplayDialog($"{ex.Message}", ResourceRetriever.GetString("ButtonTextOk"));

                await _deviceService.PopulateDevicesUi(DeviceClass.VideoCapture, ComboBoxCameras);
                await _deviceService.PopulateDevicesUi(DeviceClass.AudioCapture, ComboBoxMicrophones);

                var cameraDevice = await _deviceService.FindFirstCameraDeviceAsync();
                var microphoneDevice = await _deviceService.FindFirstMicrophoneDeviceAsync();

                await StopPreviewAsync();
                await CleanupCameraAsync();

                await InitializeCameraAsync(cameraDevice.Id, microphoneDevice.Id);

                LogHelper.Error(ex.ToString());
                Debug.WriteLine(ex);
            }
        }

        private async void ResolutionsSelection_Changed(object sender, RoutedEventArgs e)
        {
            if (!_isPreviewing || !_isInitialized || _isRecording)
            {
                return;
            }

            await StopPreviewAsync();
            await StartPreviewAsync();
        }

        private async Task InitializeCameraAsync(string cameraDeviceId = null, string microphoneDeviceId = null)
        {
            if (_mediaCapture != null)
            {
                return;
            }

            var cameraDevice = await _deviceService.FindFirstCameraDeviceAsync();
            if (!string.IsNullOrEmpty(cameraDeviceId))
            {
                cameraDevice = await _deviceService.FindDeviceAsync(cameraDeviceId);
            }

            if (cameraDevice == null)
            {
                _dialogService.DisplayDialog(ResourceRetriever.GetString("DialogTextNoCamera"), ResourceRetriever.GetString("ButtonTextOk"));
                LogHelper.Error("Camera device is null");
                return;
            }

            var microphoneDevice = await _deviceService.FindFirstMicrophoneDeviceAsync();
            if (!string.IsNullOrEmpty(microphoneDeviceId))
            {
                microphoneDevice = await _deviceService.FindDeviceAsync(microphoneDeviceId);
            }

            if (microphoneDevice == null)
            {
                _dialogService.DisplayDialog(ResourceRetriever.GetString("DialogTextNoMicrophone"), ResourceRetriever.GetString("ButtonTextOk"));
                LogHelper.Error("Microphone device is null");
                return;
            }

            _mediaCapture = new MediaCapture();

            // Register for a notification when video recording has reached the maximum time and when something goes wrong
            _mediaCapture.RecordLimitationExceeded += RecordLimitationExceeded;
            _mediaCapture.Failed += MediaCaptureFailed;

            // Initialize MediaCapture
            try
            {
                var mediaInitSettings = new MediaCaptureInitializationSettings
                {
                    VideoDeviceId = cameraDevice.Id,
                    AudioDeviceId = microphoneDevice.Id,
                    StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo,
                    SharingMode = MediaCaptureSharingMode.ExclusiveControl
                };

                await _mediaCapture.InitializeAsync(mediaInitSettings);

                _isInitialized = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                _dialogService.DisplayDialog(ResourceRetriever.GetString("DialogTextCameraAccessDenied"), ResourceRetriever.GetString("ButtonTextOk"));
                LogHelper.Error(ex.ToString());
            }

            // If initialization succeeded, start the preview
            if (_isInitialized)
            {
                if (_mediaCapture.AudioDeviceController.Muted)
                {
                    _dialogService.DisplayDialog(ResourceRetriever.GetString("DialogTextMutedMicophone"), ResourceRetriever.GetString("ButtonTextOk"),
                        ResourceRetriever.GetString("DialogTitle"));
                }

                _captureFolder = _fileService.CacheFolder();

                await _deviceService.PopulateResolutionUi(cameraDevice.Id, MediaStreamType.VideoPreview,
                    ComboBoxResolutions);

                await StartPreviewAsync();

                UpdateCaptureControls();
            }
        }

        private async Task StartPreviewAsync()
        {
            try
            {
                _displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;

                var selectedItem = ComboBoxResolutions.SelectedItem as ComboBoxItem;
                var videoEncodingProperties = (selectedItem?.Tag as StreamPropertiesHelper)?.EncodingProperties;

                await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview,
                    videoEncodingProperties);

                var streamProperties = new StreamPropertiesHelper(
                    _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview));
                PreviewControl.Width = streamProperties.Width;
                PreviewControl.Height = streamProperties.Height;
            }
            catch (UnauthorizedAccessException ex)
            {
                _dialogService.DisplayDialog(ResourceRetriever.GetString("DialogTextCameraAccessDisabled"), ResourceRetriever.GetString("ButtonTextOk"));
                LogHelper.Error(ex.ToString());
                return;
            }

            try
            {
                PreviewControl.Source = _mediaCapture;

                await _mediaCapture.StartPreviewAsync();

                _isPreviewing = true;
            }
            catch (FileLoadException ex)
            {
                _mediaCapture.CaptureDeviceExclusiveControlStatusChanged +=
                    CaptureDeviceExclusiveControlStatusChanged;
                LogHelper.Error(ex.ToString());
                Debug.WriteLine(ex);
            }
        }

        private async Task StopPreviewAsync()
        {
            if (!_isPreviewing)
            {
                return;
            }

            // Stop the preview
            _isPreviewing = false;

            await _mediaCapture.StopPreviewAsync();

            // Use the dispatcher because this method is sometimes called from non-UI threads
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                // Cleanup the UI
                PreviewControl.Source = null;

                // Allow the device screen to sleep now that the preview is stopped
                _displayRequest.RequestRelease();
            });
        }

        private async Task StartRecordingAsync()
        {
            if (!_isPreviewing)
            {
                return;
            }

            try
            {
                StartTimer();

                var selectedItem = ComboBoxResolutions.SelectedItem as ComboBoxItem;
                var videoEncodingProperties = (selectedItem?.Tag as StreamPropertiesHelper)?.EncodingProperties;

                await _mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoRecord,
                    videoEncodingProperties);

                // Create storage file for the capture
                var file =
                    await _captureFolder.CreateFileAsync($"{DateTime.Now:yyyyMMddHHmmss}{FileExtensionVideo}",
                        CreationCollisionOption.GenerateUniqueName);

                var encodingProfile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
                if (encodingProfile.Video != null)
                {
                    encodingProfile.Video.Width = DefaultVideoWidth;
                    encodingProfile.Video.Height = DefaultVideoHeight;
                    encodingProfile.Video.ProfileId = H264ProfileIds.Baseline;
                    encodingProfile.Video.Subtype = CodecSubtypes.VideoFormatH264;

                    if (ComboBoxResolutions.SelectedValue != null)
                    {
                        var streamProperties = new StreamPropertiesHelper(
                            _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview));
                        encodingProfile.Video.Width = streamProperties.Width;
                        encodingProfile.Video.Height = streamProperties.Height;
                    }
                }

                await _mediaCapture.StartRecordToStorageFileAsync(encodingProfile, file);

                _isRecording = true;

                LogHelper.Information($"Recording started with width: {encodingProfile.Video?.Width} and height: {encodingProfile.Video?.Height}");
            }
            catch (Exception ex)
            {
                // File I/O errors are reported as exceptions
                _dialogService.DisplayDialog(ResourceRetriever.GetString("DialogTextRecordingError"), ResourceRetriever.GetString("ButtonTextOk"));
                LogHelper.Error(ex.ToString());
                Debug.WriteLine(ex);
            }
        }

        private async Task StopRecordingAsync()
        {
            if (!_isRecording)
            {
                return;
            }

            _isRecording = false;

            await _mediaCapture.StopRecordAsync();

            ResetTimer();

            UpdateCaptureControls();

            LogHelper.Information("Recording was stopped");

            var progressRingPopup = new ProgressRingPopup(ResourceRetriever.GetString("DialogTextPleaseWaitWorking"));
            progressRingPopup.Show();

            LogHelper.Information("Started copying file");

            await _fileService.CopyFilesAsync();

            LogHelper.Information("Ended copying file");

            progressRingPopup.Close();
        }

        private async Task CleanupCameraAsync()
        {
            if (_isInitialized)
            {
                if (_isRecording)
                {
                    await StopRecordingAsync();
                }

                if (_isPreviewing)
                {
                    await StopPreviewAsync();
                }

                _isInitialized = false;
            }

            if (_mediaCapture != null)
            {
                _mediaCapture.RecordLimitationExceeded -= RecordLimitationExceeded;
                _mediaCapture.Failed -= MediaCaptureFailed;
                _mediaCapture.Dispose();
                _mediaCapture = null;
            }
        }

        private void UpdateCaptureControls()
        {
            // The buttons should only be enabled if the preview started sucessfully
            RecordButton.IsEnabled = _isPreviewing;
            PauseButton.IsEnabled = _isRecording;

            // Update recording button to show "Stop" icon instead of red "Record" icon
            StartRecordingIcon.Visibility = _isRecording ? Visibility.Collapsed : Visibility.Visible;
            StopRecordingIcon.Visibility = _isRecording ? Visibility.Visible : Visibility.Collapsed;

            // Pause button
            PauseButton.Visibility = _isRecording ? Visibility.Visible : Visibility.Collapsed;

            // Show "Rec" and timer if recording
            TextBlockRecording.Visibility = _isRecording ? Visibility.Visible : Visibility.Collapsed;
            TextBlockTimer.Visibility = _isRecording ? Visibility.Visible : Visibility.Collapsed;

            // Hide cameras, microphones and resolutions if recording
            GridControls.Visibility = _isRecording ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void CaptureDeviceExclusiveControlStatusChanged(MediaCapture sender,
            MediaCaptureDeviceExclusiveControlStatusChangedEventArgs args)
        {
            if (args.Status != MediaCaptureDeviceExclusiveControlStatus.SharedReadOnlyAvailable)
            {
                if (args.Status != MediaCaptureDeviceExclusiveControlStatus.ExclusiveControlAvailable || _isPreviewing)
                {
                    return;
                }

                async void AgileCallback()
                {
                    await StartPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    AgileCallback);
            }
            else
            {
                _dialogService.DisplayDialog(ResourceRetriever.GetString("DialogTextCaptureDeviceExclusiveControl"), ResourceRetriever.GetString("ButtonTextOk"));
            }
        }

        private async void RecordLimitationExceeded(MediaCapture sender)
        {
            await StopRecordingAsync();
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateCaptureControls);
        }

        private async void MediaCaptureFailed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            try
            {
                await CleanupCameraAsync();
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateCaptureControls);
            }
            catch (Exception ex)
            {
                _dialogService.DisplayDialog(
                    ResourceRetriever.GetString("DialogTextMediaCaptureFailed"),
                    ResourceRetriever.GetString("ButtonTextOk"));
                LogHelper.Error(ex.ToString());
                Debug.WriteLine(ex);
            }
        }

        private async void CurrentSuspending(object sender, SuspendingEventArgs e)
        {
            Debug.WriteLine("Current Suspending");

            // Handle global application events only if this page is active
            if (Frame.CurrentSourcePageType != typeof(CapturePage))
            {
                return;
            }

            await CleanupCameraAsync();

            e.SuspendingOperation.GetDeferral().Complete();
        }

        private void StartTimer()
        {
            if (_timer == null)
            {
                _timer = new DispatcherTimer();
                _timer.Tick += DispatchTimerTick;
                _timer.Interval = new TimeSpan(0, 0, 1);
            }

            _timer.Start();
        }

        private void PauseTimer()
        {
            _timer?.Stop();
        }

        private void ResetTimer()
        {
            if (_timer == null)
            {
                return;
            }

            _timer.Stop();
            _timer.Tick -= DispatchTimerTick;
            _timer = null;

            _secondsCount = 0;
            TextBlockTimer.Text = "00:00:00";
        }

        private async void DispatchTimerTick(object sender, object e)
        {
            _secondsCount++;

            var timeSpan = TimeSpan.FromSeconds(_secondsCount);
            TextBlockTimer.Text = timeSpan.ToString(@"hh\:mm\:ss");

            if (Math.Abs(timeSpan.TotalMinutes - MaxDurationInMinutes) < 0.000000001)
            {
                await StopRecordingAsync();
            }
        }
    }
}