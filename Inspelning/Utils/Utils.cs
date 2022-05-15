using System.Text.RegularExpressions;
using Windows.ApplicationModel;

namespace Inspelning.Recorder.Utils
{
    internal static class Utils
    {
        public static string GetAppVersion()
        {
            var package = Package.Current;
            var packageId = package.Id;
            var version = packageId.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }
}