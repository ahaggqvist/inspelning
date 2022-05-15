using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Inspelning.Recorder.Utils;

namespace Inspelning.Recorder.Services
{
    public class DialogService : IDialogService
    {
        public void DisplayDialog(string content, string buttonText, string title = "Ett fel har uppstått")
        {
            _ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    var dialog = new ContentDialog
                    {
                        Title = title,
                        Content = content,
                        PrimaryButtonText = buttonText
                    };

                    _ = dialog.EnqueueAndShowIfAsync();
                });
        }
    }
}