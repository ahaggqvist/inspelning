using System;
using Windows.UI.Xaml.Controls;

namespace Inspelning.Recorder.Services
{
    public class NavigateService : INavigateService
    {
        private static readonly Lazy<NavigateService> Lazy = new(() => new NavigateService());

        public static NavigateService Instance => Lazy.Value;

        public Frame Frame { get; set; }

        public void Navigate(Type type)
        {
            Frame.Navigate(type);
        }

        public void Navigate(Type type, Type masterPageType)
        {
            var masterPage = Frame.Content as Page;
            if (masterPage != null && masterPage.GetType() != masterPageType)
            {
                Frame.Navigate(masterPageType);
                masterPage = Frame.Content as Page;
            }

            var contentFrame = masterPage?.FindName("ContentFrame") as Frame;
            contentFrame?.Navigate(type);
        }
    }
}