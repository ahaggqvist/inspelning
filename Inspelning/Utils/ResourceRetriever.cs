using Windows.ApplicationModel.Resources.Core;

namespace Inspelning.Recorder.Utils
{
    internal static class ResourceRetriever
    {
        private static ResourceMap _resourceMap;

        public static ResourceMap ResourceMap
        {
            get { return _resourceMap ??= ResourceManager.Current.MainResourceMap; }
        }

        public static string GetString(string key)
        {
            return ResourceMap?.GetValue("Resources/" + key, new ResourceContext())?.ValueAsString;
        }

        public static string GetString(string key, ResourceContext context)
        {
            return ResourceMap?.GetValue("Resources/" + key, context)?.ValueAsString;
        }
    }
}