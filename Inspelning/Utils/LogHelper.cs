using System.IO;
using Windows.Storage;
using Serilog;

namespace Inspelning.Recorder.Utils
{
    public class LogHelper
    {
        private const string LogPath = "logs/log.txt";
        private static bool _isInitialized;

        public static void Information(string message)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            Log.Information(message);
        }

        public static void Debug(string message)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            Log.Debug(message);
        }

        public static void Warning(string message)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            Log.Warning(message);
        }

        public static void Error(string message)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            Log.Error(message);
        }

        private static void Initialize()
        {
            var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, LogPath);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(filePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _isInitialized = true;
        }
    }
}