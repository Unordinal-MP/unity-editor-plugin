using System;
using UnityEditor;

namespace Unordinal.Hosting
{
    public static class BundleArchitectures
    {
        public static readonly BundleArchitecture Intel64 = new BundleArchitecture("server.x86_64", BuildTarget.StandaloneLinux64);
    }

    public struct BundleArchitecture
    {
        public string Identifier { get; }
        public BuildTarget BuildTarget { get; }

        internal BundleArchitecture(string identifier, BuildTarget buildTarget)
        {
            this.Identifier = identifier;
            this.BuildTarget = buildTarget;
        }
    }
}
