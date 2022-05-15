namespace Inspelning.Recorder.Services
{
    public interface ISettingService
    {
        void SaveSetting(string key, string value);
        string LoadSetting(string key);
    }
}