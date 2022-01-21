using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace Unordinal.Hosting
{
    public class TarGzArchiver
    {
        public async Task CreateTarGZAsync(string tgzFilename, string sourceDirectory, CancellationToken token)
        {
            await Task.Run(() => CreateTarGZ(tgzFilename, sourceDirectory, token));
        }

        public void CreateTarGZ(string tgzFilename, string sourceDirectory, CancellationToken token)
        {
            var outStream = File.Create(tgzFilename);
            var gzoStream = new GZipOutputStream(outStream);
            var tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);

            // Note that the RootPath is currently case sensitive and must be forward slashes e.g. "c:/temp"
            // and must not end with a slash, otherwise cuts off first char of filename
            // This is scheduled for fix in next release
            tarArchive.RootPath = sourceDirectory.Replace('\\', '/');
            if (tarArchive.RootPath.EndsWith("/"))
                tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);

            addDirectoryFilesToTar(tarArchive, sourceDirectory, true, token);

            tarArchive.Close();
        }

        private void addDirectoryFilesToTar(TarArchive tarArchive, string sourceDirectory, bool recurse, CancellationToken token)
        {
            // Optionally, write an entry for the directory itself.
            // Specify false for recursion here if we will add the directory's files individually.
            TarEntry tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
            tarArchive.WriteEntry(tarEntry, false);

            // Write each file to the tar.
            string[] filenames = Directory.GetFiles(sourceDirectory);
            foreach (string filename in filenames)
            {
                if (token.IsCancellationRequested) { return; } // Deployment has been canceled.

                tarEntry = TarEntry.CreateEntryFromFile(filename);
                tarArchive.WriteEntry(tarEntry, true);
            }

            if (recurse)
            {
                string[] directories = Directory.GetDirectories(sourceDirectory);
                foreach (string directory in directories)
                    addDirectoryFilesToTar(tarArchive, directory, recurse, token);
            }
        }
    }
}
