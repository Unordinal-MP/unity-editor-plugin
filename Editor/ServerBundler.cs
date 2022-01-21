using System.IO;
using System.Threading.Tasks;

namespace Unordinal.Hosting
{
    public class ServerBundler
    {
        private const string RootPath = "Hosting_Builds";

        private static string HostingBuildPath = Path.Combine(RootPath, "Server");

        private static string DockerFilePath = Path.Combine(RootPath, "Dockerfile");

        private readonly TarGzArchiver archiver;

        private readonly UnityServerBuilder unityBuilder;

        public ServerBundler(TarGzArchiver archiver, UnityServerBuilder unityBuilder)
        {
            this.archiver = archiver;
            this.unityBuilder = unityBuilder;
        }

        public string Bundle(BundleArchitecture architecture)
        {
            CleanupBeforePreviousBuilds();
            unityBuilder.BuildServer(Path.Combine(HostingBuildPath, architecture.Identifier), architecture);
            File.WriteAllText(DockerFilePath, GetDockerFileContent(architecture));
            return RootPath;
        }

        public Task<string> BundleAsync(BundleArchitecture architecture)
        {
            return Task.Run(() => Bundle(architecture));
        }

        private static void CleanupBeforePreviousBuilds()
        {
            if (Directory.Exists(HostingBuildPath))
            {
                Directory.Delete(HostingBuildPath, true);
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
                USER unity
                CMD [""/bin/bash"", ""-c"", ""exec ./server/{architecture.Identifier} -batchmode -nographics 2>&1 | (sudo tee -a /aci/logs/unity_server.log)""]
            ";
        }
    }
}
