using Windows.Storage;

namespace Inspelning.Recorder.Services
{
    public class SettingService : ISettingService
    {
        public void SaveSetting(string key, string value)
        {
            ApplicationData.Current.LocalSettings.Values[key] = value;
        }

        public string LoadSetting(string key)
        {
            return ApplicationData.Current.LocalSettings.Values[key] as string;
        }
    }
}