using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Inspelning.Recorder.CustomControls
{
    public sealed partial class ProgressRingPopup
    {
        private Popup _popup;

        public ProgressRingPopup()
        {
            InitializeComponent();
        }

        public ProgressRingPopup(string message)
        {
            InitializeComponent();
            SystemNavigationManager.GetForCurrentView().BackRequested += ProgressRingPopup_BackRequested;
            Window.Current.CoreWindow.SizeChanged += CoreWindow_SizeChanged;
            TextBlockMessage.Text = message;
        }

        private void CoreWindow_SizeChanged(CoreWindow sender, WindowSizeChangedEventArgs args)
        {
            UpdateUi();
        }

        private void ProgressRingPopup_BackRequested(object sender, BackRequestedEventArgs e)
        {
            Close();
        }

        private void UpdateUi()
        {
            var bounds = Window.Current.Bounds;
            Width = bounds.Width;
            Height = bounds.Height;
        }

        public void Show()
        {
            _popup = new Popup
            {
                Child = this
            };
            ProgressRingProgress.IsActive = true;
            _popup.IsOpen = true;
            UpdateUi();
        }


        public void Close()
        {
            if (!_popup.IsOpen)
            {
                return;
            }

            ProgressRingProgress.IsActive = false;
            _popup.IsOpen = false;
            SystemNavigationManager.GetForCurrentView().BackRequested -= ProgressRingPopup_BackRequested;
            Window.Current.CoreWindow.SizeChanged -= CoreWindow_SizeChanged;
        }
    }
}