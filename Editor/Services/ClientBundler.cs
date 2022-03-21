using System.IO;
using UnityEditor;

namespace Unordinal.Editor.Services
{
    public class ClientBundler
    {
        private static readonly string RootPath = Path.Combine(Constants.buildFolder, "Client");

        private static readonly string HostingBuildPath = Path.Combine(RootPath, "Client");

        private static readonly string RestoreBuildPath = Path.Combine(Constants.buildFolder, "Dummy", "Dummy");

        private readonly UnityBuilder unityBuilder;

        public ClientBundler(UnityBuilder unityBuilder)
        {
            this.unityBuilder = unityBuilder;
        }

        public void RestoreBuildTarget(BuildTargetGroup originalGroup, BuildTarget originalTarget)
        {
            unityBuilder.Build(RestoreBuildPath, originalGroup, originalTarget, new string[] { });
        }

        public string Bundle(BuildTargetGroup originalGroup, BuildTarget originalTarget)
        {
            CleanupBeforePreviousBuilds();
            unityBuilder.Build(HostingBuildPath, originalGroup, originalTarget, UnityBuilder.CollectScenes(WhichBuild.Client));
            return RootPath;
        }

        private static void CleanupBeforePreviousBuilds()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, true);
            }
        }
    }
}
