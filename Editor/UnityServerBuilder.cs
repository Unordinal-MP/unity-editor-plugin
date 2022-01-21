using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using UnityEditor;

namespace Unordinal.Hosting
{
    public class UnityServerBuilder
    {
        private readonly ILogger<UnityServerBuilder> logger;

        public UnityServerBuilder(ILogger<UnityServerBuilder> logger) {
            this.logger = logger;
        }

        public void BuildServer(string path, BundleArchitecture architecture)
        {
            VerifyArchitecture(architecture);
            string[] scenes = CollectScenes();
            BuildPlayerOptions options = PrepareOptions(path, architecture, scenes);
            Build(options);
        }

        private static void VerifyArchitecture(BundleArchitecture architecture)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, architecture.BuildTarget))
            {
                throw new BuildException($"It seems that standalone {architecture.BuildTarget} is not available to build for. Please add the module in Unity Hub!");
            }
        }

        private static string[] CollectScenes()
        {
            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(s => s.path).ToArray();
            if (scenes.Count() == 0)
            {
                throw new BuildException("It seems that no scenes have been added. Please check the Build Settings (ctrl+shift+B) and try again. ");
            }
            return scenes;
        }

        private void Build(BuildPlayerOptions options)
        {
            logger.LogDebug("Building server...");
            var buildReport = BuildPipeline.BuildPlayer(options);
            logger.LogDebug("Server built in {0}", buildReport.summary.totalTime);
            if (buildReport.summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
            {
                throw new BuildException($"Build failed with {buildReport.summary.totalErrors} error(s)");
            }
        }

        private static BuildPlayerOptions PrepareOptions(string path, BundleArchitecture architecture, string[] scenes)
        {
            return new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = architecture.BuildTarget,
                options = BuildOptions.EnableHeadlessMode
            };
        }
    }

    public class BuildException: Exception
    {
        public BuildException(string message) : base(message) { }
    }
}
