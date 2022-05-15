using System;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Inspelning.Recorder.Services;
using Unity;
using static Inspelning.Recorder.Utils.Utils;

namespace Inspelning.Recorder
{
    public sealed partial class SettingsPage
    {
        private readonly IDialogService _dialogService;
        private readonly ISettingService _settingService;

        public SettingsPage() : this(App.Container.Resolve<SettingService>(), App.Container.Resolve<DialogService>())
        {
        }

        public SettingsPage(ISettingService settingService, IDialogService dialogService)
        {
            _settingService = settingService;
            _dialogService = dialogService;

            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            TextBlockAppDataPath.Text = ApplicationData.Current.LocalFolder.Path;
            TextBlockPackageVersion.Text = GetAppVersion();
        }

        private async void OpenExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchFolderAsync
                (ApplicationData.Current.LocalFolder);
        }
    }
}