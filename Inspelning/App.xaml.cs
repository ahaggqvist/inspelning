using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.ExtendedExecution.Foreground;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Inspelning.Recorder.Services;
using Inspelning.Recorder.Utils;
using Unity;
using UnhandledExceptionEventArgs = Windows.UI.Xaml.UnhandledExceptionEventArgs;

namespace Inspelning.Recorder
{
    /// <summary>
    ///     Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App
    {
        private ExtendedExecutionForegroundSession _session;

        /// <summary>
        ///     Initializes the singleton application object.  This is the first line of authored code
        ///     executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;

            Container.RegisterType<IDialogService>();
        }

        public static IUnityContainer Container { get; set; } = new UnityContainer();

        /// <summary>
        ///     Invoked when the application is launched normally by the end user.  Other entry points
        ///     will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (Window.Current.Content is not Frame rootFrame)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            NavigateService.Instance.Frame = rootFrame;

            if (e.PrelaunchActivated)
            {
                return;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }

            // Ensure the current window is active
            Window.Current.Activate();

            if (_session == null)
            {
                await PreventFromSuspending();
            }
        }

        /// <summary>
        ///     Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = ResourceRetriever.GetString("DialogTitleError"),
                        Content = ResourceRetriever.GetString("DialogTextNavigationFailed"),
                        PrimaryButtonText = ResourceRetriever.GetString("ButtonTextOk")
                    };

                    _ = dialog.ShowAsync();
                });
            LogHelper.Error($"Failed to load Page {e.SourcePageType.FullName}");
        }

        /// <summary>
        ///     Invoked when application execution is being suspended.  Application state is saved
        ///     without knowing whether the application will be terminated or resumed with the contents
        ///     of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = ResourceRetriever.GetString("DialogTitleError"),
                        Content = $"{unhandledExceptionEventArgs.Message}.",
                        PrimaryButtonText = ResourceRetriever.GetString("ButtonTextOk")
                    };
                    _ = dialog.EnqueueAndShowIfAsync();
                });

            LogHelper.Error($"UnhandledException {unhandledExceptionEventArgs.Message}");
            Debug.WriteLine(unhandledExceptionEventArgs.Exception);
        }

        private async Task PreventFromSuspending()
        {
            var newSession = new ExtendedExecutionForegroundSession
            {
                Reason = ExtendedExecutionForegroundReason.Unconstrained
            };
            newSession.Revoked += SessionRevoked;

            var result = await newSession.RequestExtensionAsync();
            switch (result)
            {
                case ExtendedExecutionForegroundResult.Allowed:
                    _session = newSession;
                    break;
                default:
                case ExtendedExecutionForegroundResult.Denied:
                    newSession.Dispose();
                    break;
            }
        }

        private void SessionRevoked(object sender, ExtendedExecutionForegroundRevokedEventArgs args)
        {
            if (_session == null)
            {
                return;
            }

            _session.Dispose();
            _session = null;
        }
    }
}