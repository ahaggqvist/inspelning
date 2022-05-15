using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Inspelning.Recorder.Utils;

namespace Inspelning.Recorder.Services
{
    public class DeviceService : IDeviceService
    {
        private const int MaxWidthResolution = 1280;
        private const int MinWidthResolution = 640;
        private const int MinFrameRate = 25;

        public async Task PopulateResolutionUi(string deviceInterfaceId, MediaStreamType streamType, Selector comboBox,
            bool showFrameRate = true)
        {
            var streamProperties = await FindStreamProperties(deviceInterfaceId, streamType);
            foreach (var propertiesHelper in streamProperties)
            {
                var comboBoxItem = new ComboBoxItem
                {
                    Content = propertiesHelper.GetFriendlyName(showFrameRate),
                    Tag = propertiesHelper
                };
                comboBox.Items?.Add(comboBoxItem);
            }

            if (comboBox.Items is { Count: > 0 })
            {
                comboBox.SelectedIndex = comboBox.Items.Count - 1;
            }
            else
            {
                comboBox.SelectedIndex = 0;
            }
        }

        public async Task PopulateDevicesUi(DeviceClass deviceClass, Selector comboBox)
        {
            comboBox.Items?.Clear();

            var deviceInformations = await DeviceInformation.FindAllAsync(deviceClass);
            foreach (var deviceInformation in deviceInformations)
            {
                var comboBoxItem = new ComboBoxItem
                {
                    Content = deviceInformation.Name,
                    Tag = deviceInformation.Id
                };
                comboBox.Items?.Add(comboBoxItem);
            }

            comboBox.SelectedIndex = 0;
        }

        public async Task<DeviceInformation> FindDeviceAsync(string deviceId)
        {
            return await DeviceInformation.CreateFromIdAsync(deviceId);
        }

        public async Task<DeviceInformation> FindFirstCameraDeviceAsync()
        {
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            return devices.FirstOrDefault();
        }

        public async Task<DeviceInformation> FindFirstMicrophoneDeviceAsync()
        {
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.AudioCapture);
            return devices.FirstOrDefault();
        }

        private async Task<IOrderedEnumerable<StreamPropertiesHelper>> FindStreamProperties(string deviceInterfaceId,
            MediaStreamType streamType)
        {
            var captureInitSettings = new MediaCaptureInitializationSettings
            {
                VideoDeviceId = deviceInterfaceId
            };

            var mediaCapture = new MediaCapture();
            await mediaCapture.InitializeAsync(captureInitSettings);

            var streamProperties =
                mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(streamType)
                    .Select(x => new StreamPropertiesHelper(x))
                    .Where(x => x.Width >= MinWidthResolution && x.Width <= MaxWidthResolution &&
                                x.FrameRate >= MinFrameRate)
                    .OrderByDescending(x => x.Height * x.Width)
                    .ThenByDescending(x => x.FrameRate);

            return streamProperties;
        }
    }
}