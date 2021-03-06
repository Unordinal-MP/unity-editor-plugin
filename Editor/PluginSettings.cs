namespace Unordinal.Hosting
{
    public static class PluginSettings
    {
        public static string GetPluginVersion()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unordinal.hosting/package.json");
            return packageInfo.version;
        }

        public const string ApiBaseUrl = "https://api.unordinal.com/api/v1/";
        public const string Auth0BaseUrl = "https://auth.unordinal.com";
        public const string ClientId = "cO1dolaPZIzxuczexcxFiQW7vN3kGNJv";
        public const string Audience = "EnYxLQuRyiT0VsLfpsRYyO4HhNnLxpCf";
        public const string MeasurementID = "G-4L4H0WJEGX"; 
        public const string APISecret = "hoHQfKfLQi-LJhBV2tmCMA";
    }
}
