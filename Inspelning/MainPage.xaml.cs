using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Inspelning.Recorder.Services;
using Inspelning.Recorder.Utils;
using Unity;
using static Inspelning.Recorder.Utils.Extensions;

namespace Inspelning.Recorder
{
    public sealed partial class MainPage
    {
        private const int MinFreeSpaceRemaining = 20;
        private readonly IDialogService _dialogService;
        private readonly IFileService _fileService;
        private readonly ISettingService _settingService;

        public MainPage() : this(App.Container.Resolve<DialogService>(), App.Container.Resolve<SettingService>(), App.Container.Resolve<FileService>())
        {
        }

        public MainPage(IDialogService dialogService, ISettingService settingService, IFileService fileService)
        {
            InitializeComponent();

            _dialogService = dialogService;
            _settingService = settingService;
            _fileService = fileService;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Copy existing recordings
            await _fileService.CopyFilesAsync();

            // Check free space
            var freeSpaceRemaining = await _fileService.FreeSpaceRemaining();
            if (freeSpaceRemaining / 1024 / 1024 / 1024 >= MinFreeSpaceRemaining)
            {
                return;
            }

            var s = freeSpaceRemaining.ToSize(SizeUnits.Gb);
            _dialogService.DisplayDialog($"{ResourceRetriever.GetString("DialogTextFreeSpace")} {s} GB.", ResourceRetriever.GetString("ButtonTextOk"),
                ResourceRetriever.GetString("DialogTitle"));
        }

        private void RecordVideoButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateService.Instance.Navigate(typeof(CapturePage), typeof(MainPage));
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateService.Instance.Navigate(typeof(SettingsPage), typeof(MainPage));
        }
    }
}