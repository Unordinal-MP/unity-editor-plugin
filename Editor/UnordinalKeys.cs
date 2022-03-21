using UnityEditor;

namespace Unordinal.Editor
{
    public static class UnordinalKeys
    {
        // Token storage keys
        public static string tokenKey = SharedUniqueKey("OAuthToken");
        public static string refreshTokenKey = SharedUniqueKey("OAuthRefreshToken");
        public static string tokenExpirationDateKey = SharedUniqueKey("OAuthTokenExpDate");
        // UserContextProvider key
        public static string userInfoKey = SharedUniqueKey("OAuthUserInfo");
        // Progress estimator
        public static string zipTimeKey = ProjectUniqueKey("estimatedSecondsZipping");
        public static string dockerTimeKey = ProjectUniqueKey("estimatedSecondsBuildingDockerImage");
        public static string uploadClientUrlTimeKey = ProjectUniqueKey("estimatedSecondsGetClientUploadUrl");
        public static string uploadClientTimeKey = ProjectUniqueKey("estimatedSecondsUploadClient");
        public static string uploadServerUrlTimeKey = ProjectUniqueKey("estimatedSecondsGetUploadUrl");
        public static string uploadServerTimeKey = ProjectUniqueKey("estimatedSecondsUploadFile");
        public static string buildingImageTimeKey = ProjectUniqueKey("estimatedSecondsBuildingImage");
        public static string pingRegionsTimeKey = ProjectUniqueKey("estimatedSecondsPingRegions");
        public static string deployingTimeKey = ProjectUniqueKey("estimatedSecondsDeploying");
        public static string zipPercentageKey = ProjectUniqueKey("percentageZipping");
        public static string dockerPercentageKey = ProjectUniqueKey("percentageBuildingDockerImage");
        public static string uploadClientUrlPercentageKey = ProjectUniqueKey("percentageGetClientUploadUrl");
        public static string uploadClientPercentageKey = ProjectUniqueKey("percentageUploadClient");
        public static string uploadServerUrlPercentageKey = ProjectUniqueKey("percentageGetUploadUrl");
        public static string uploadServerPercentageKey = ProjectUniqueKey("percentageUploadFile");
        public static string buildingImagePercentageKey = ProjectUniqueKey("percentageBuildingImage");
        public static string pingRegionsPercentageKey = ProjectUniqueKey("percentagePingRegions");
        public static string deployingPercentageKey = ProjectUniqueKey("percentageDeploying");
        // Start page
        public static string serverStartSceneKey = ProjectUniqueKey("ServerStartScene");
        public static string clientStartSceneKey = ProjectUniqueKey("ClientStartScene");
        public static string playWithFriendsEnabledKey = ProjectUniqueKey("PlayWithFriendsEnabled");
        public static string OptionsVisibleKey = ProjectUniqueKey("OptionsVisible");
        // Deployment
        public static string shouldDoADeployKey = ProjectUniqueKey("ShouldDoADeploy");
        public static string deployInNextUpdateKey = ProjectUniqueKey("DeployInNextUpdate");
        public static string deploymentGuidKey = ProjectUniqueKey("deploymentGuidKey");
        // Error page
        public static string errorMainMessageKey = ProjectUniqueKey("ErrorMainMessageKey");
        public static string errorDetailedMessageKey = ProjectUniqueKey("ErrorDetailedMessageKey");

        // The prefix make it Plugin specific.
        private static string UnordinalPrefix = "Unordinal";

        private static string SharedUniqueKey(string key)
        {
            return $"{UnordinalPrefix}_{key}";
        }

        /// <summary>
        /// Adds unique prefix per product to the desired key.
        /// </summary>
        /// <param name="key">Key to add prefix to.</param>
        /// <returns></returns>
        private static string ProjectUniqueKey(string key)
        {
            // Part that makes key unique per project.
            var projectSpecific = $"{ PlayerSettings.companyName }_{ PlayerSettings.productName}";

            return $"{UnordinalPrefix}_{projectSpecific}_{key}";
        }

        /// <summary>
        /// Returns the unique key for the given build.
        /// </summary>
        /// <param name="build">Build to get key for.</param>
        /// <returns></returns>
        public static string PlatformKey(WhichBuild build)
        {
            return ProjectUniqueKey($"{build}-key");
        }

        // Clear all keys
        public static void ClearEditorPrefs()
        {
            var fields = typeof(UnordinalKeys).GetFields();
            foreach(var field in fields)
            {
                if (field.FieldType == typeof(string))
                {
                    // This might be an EditorPrefs key.

                    object outValue = null;
                    field.GetValue(outValue);
                    var key = System.Convert.ToString(outValue);
                    if (EditorPrefs.HasKey(key))
                    {
                        EditorPrefs.DeleteKey(key);
                    }
                }
            }
        }

        public static void ClearSignInInformation()
        {
            EditorPrefs.DeleteKey(tokenKey);
            EditorPrefs.DeleteKey(refreshTokenKey);
            EditorPrefs.DeleteKey(tokenExpirationDateKey);
    }
    }
}
