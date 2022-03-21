using System;
using UnityEditor;

namespace Unordinal.Editor
{
    public static class BundleArchitectures
    {
        public static readonly BundleArchitecture Intel64 = new BundleArchitecture("server.x86_64", BuildTarget.StandaloneLinux64);
        public static readonly BundleArchitecture Default = new BundleArchitecture("client", BuildTarget.StandaloneWindows64, true);
    }

    public struct BundleArchitecture
    {
        public string Identifier { get; }
        public BuildTarget BuildTarget { get; }
        public bool IsDefaultForClient { get; }

        internal BundleArchitecture(string identifier, BuildTarget buildTarget, bool isDefault = false)
        {
            this.Identifier = identifier;
            this.BuildTarget = buildTarget;
            this.IsDefaultForClient = isDefault;
        }
    }

    public enum WhichBuild
    {
        Server,
        Client,
    }
}
