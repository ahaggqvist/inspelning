using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Controls;

namespace Inspelning.Recorder.Utils
{
    public static class ContentDialogExtensions
    {
        private static TaskCompletionSource<Null> _previousDialogCompletion;

        public static async Task<ContentDialogResult> EnqueueAndShowIfAsync(this ContentDialog contentDialog,
            Func<bool> predicate = null)
        {
            var currentDialogCompletion = new TaskCompletionSource<Null>();

            // No locking needed since we are always on the UI thread
            if (!CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
                throw new NotSupportedException("Can only show dialog from UI thread.");
            }

            var previousDialogCompletion = _previousDialogCompletion;
            _previousDialogCompletion = currentDialogCompletion;

            if (previousDialogCompletion != null)
            {
                await previousDialogCompletion.Task;
            }

            var whichButtonWasPressed = ContentDialogResult.None;
            if (predicate == null || predicate())
            {
                whichButtonWasPressed = await contentDialog.ShowAsync();
            }

            currentDialogCompletion.SetResult(null);
            return whichButtonWasPressed;
        }
    }
}