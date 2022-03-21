using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using UnityEditor;
using UnityEngine;

namespace Unordinal.Editor.Services
{
    public class UnityBuilder
    {
        private readonly ILogger<UnityBuilder> logger;

        public UnityBuilder(ILogger<UnityBuilder> logger) {
            this.logger = logger;
        }

        public void Build(string path, BundleArchitecture architecture, string[] scenes)
        {
            //TODO: would be better with overloaded equals
            if (!architecture.IsDefaultForClient)
                VerifyArchitecture(architecture);
            BuildPlayerOptions options = PrepareOptions(path, architecture, scenes);
            BuildInternal(options);
        }

        public void Build(string path, BuildTargetGroup originalGroup, BuildTarget originalTarget, string[] scenes)
        {
            BuildPlayerOptions options = PrepareOptions(path, BundleArchitectures.Default, scenes);
            options.targetGroup = originalGroup;
            options.target = originalTarget;
            BuildInternal(options);
        }

        private static void VerifyArchitecture(BundleArchitecture architecture)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, architecture.BuildTarget))
            {
                throw new BuildException($"It seems that standalone {architecture.BuildTarget} is not available to build for. Please add the module in Unity Hub!");
            }
        }

        public static string[] CollectScenes(WhichBuild whichBuild)
        {
            if(!EditorBuildSettings.scenes.Any())
            {
                throw new BuildException("It seems that no scenes have been added. Please check the Build Settings (ctrl+shift+B) and try again. ");
            }

            // Get all scenes paths for the scenes in EditorBuildSettings that are also stored in EditorPrefs.
            var editorPrefsKey = UnordinalKeys.PlatformKey(whichBuild);
            var scenes = EditorBuildSettings.scenes.Where(scene =>
                EditorPrefs.HasKey(editorPrefsKey) ? EditorPrefs.GetString(editorPrefsKey).Split(',').ToList().Contains(scene.path) : false)
                .Select(s => s.path).ToArray();

            if (!scenes.Any())
            {
                // Scenes are added to build settings, but we dont have any scene selected in plugin.
                // Get all the scenes in build settings.
                scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
            }
            return scenes;
        }

        private void BuildInternal(BuildPlayerOptions options)
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
            BuildTarget target = architecture.BuildTarget;
            BuildOptions options = default;
            if (architecture.IsDefaultForClient)
            {
                target = EditorUserBuildSettings.activeBuildTarget;
            }
            else
            {
                options = BuildOptions.EnableHeadlessMode;
            }

            if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
            {
                path += ".exe";
            }

            return new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = target,
                options = options,
            };
        }
    }

    public class BuildException: Exception
    {
        public BuildException(string message) : base(message) { }
    }
}
