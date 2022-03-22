using System.IO;
namespace Unordinal.Editor.Services
{
    public class ServerBundler
    {
        private static readonly string RootPath = Path.Combine(Constants.buildFolder, "Server");

        private static readonly string HostingBuildPath = Path.Combine(RootPath, "Server");

        private static readonly string DockerFilePath = Path.Combine(RootPath, "Dockerfile");

        private readonly UnityBuilder unityBuilder;

        public ServerBundler(UnityBuilder unityBuilder)
        {
            this.unityBuilder = unityBuilder;
        }

        public string Bundle(BundleArchitecture architecture)
        {
            CleanupBeforePreviousBuilds();
            unityBuilder.Build(Path.Combine(HostingBuildPath, architecture.Identifier), architecture, UnityBuilder.CollectScenes(WhichBuild.Server));
            var buildSucceeded = Directory.Exists(HostingBuildPath);
            if(!buildSucceeded)
            {
                // When building server without Linux support, it's possible to get a succeeded build report even though a server was never built.
                throw new System.Exception("Server build failed, make sure you have Linux build support added. A restart of Unity might also be needed.");
            }
            File.WriteAllText(DockerFilePath, GetDockerFileContent(architecture));
            return RootPath;
        }

        private static void CleanupBeforePreviousBuilds()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, true);
            }
        }

        private static string GetDockerFileContent(BundleArchitecture architecture)
        {
            return $@"
                FROM ubuntu:latest
                RUN apt-get update -qq; \
                apt-get install -qq -y curl \
                    && apt-get install -y sudo \
                    && apt-get clean \
                    && rm -rf /var/lib/apt/lists/*
                COPY Server server
                RUN useradd -ms /bin/bash unity
                RUN adduser unity sudo
                RUN echo '%sudo ALL=(ALL) NOPASSWD:ALL' >> /etc/sudoers
                RUN chown unity:unity -R server/{architecture.Identifier}
                RUN chmod -R 755 server
                RUN chown unity:unity -R /server
                USER unity
                WORKDIR /server
                CMD [""/bin/bash"", ""-c"", ""exec ./{architecture.Identifier} -batchmode -nographics 2>&1 | (sudo tee -a /aci/logs/unity_server.log)""]
            ";
        }
    }
}
