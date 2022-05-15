using System;
using Windows.UI.Xaml.Controls;

namespace Inspelning.Recorder.Services
{
    public interface INavigateService
    {
        Frame Frame { get; set; }
        void Navigate(Type type);
        void Navigate(Type type, Type masterPageType);
    }
}