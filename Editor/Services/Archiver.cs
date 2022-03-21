using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using Unordinal.Editor.Utils;

namespace Unordinal.Editor.Services
{
    public class Archiver
    {
        public async Task CreateZipAsync(string zipFilename, string sourceDirectory, CancellationToken token)
        {
            await TaskHelpers.RunAsync(() => CreateZip(zipFilename, sourceDirectory, token));
        }

        public void CreateZip(string zipFilename, string sourceDirectory, CancellationToken token)
        {
            using (var zipArchive = ZipFile.Create(zipFilename))
            {
                zipArchive.BeginUpdate();

                AddDirectoryFilesToZip(zipArchive, sourceDirectory, true, token);

                zipArchive.CommitUpdate();
            }
        }

        private void AddDirectoryFilesToZip(ZipFile zipArchive, string sourceDirectory, bool recurse, CancellationToken token)
        {
            string[] filenames = Directory.GetFiles(sourceDirectory);
            foreach (string filename in filenames)
            {
                if (token.IsCancellationRequested) { return; } // Deployment has been canceled.
                string path = filename.Replace('\\', '/');
                int prefixIndex = path.IndexOf("/");
                string entryName = path.Substring(prefixIndex + 1);
                zipArchive.Add(path, entryName);
            }

            if (recurse)
            {
                string[] directories = Directory.GetDirectories(sourceDirectory);
                foreach (string directory in directories)
                    AddDirectoryFilesToZip(zipArchive, directory, recurse, token);
            }
        }

        public async Task CreateTarGZAsync(string tgzFilename, string sourceDirectory, CancellationToken token)
        {
            await TaskHelpers.RunAsync(() => CreateTarGZ(tgzFilename, sourceDirectory, token));
        }

        public void CreateTarGZ(string tgzFilename, string sourceDirectory, CancellationToken token)
        {
            using (var outStream = File.Create(tgzFilename))
            using (var gzoStream = new GZipOutputStream(outStream))
            using (var tarArchive = TarArchive.CreateOutputTarArchive(gzoStream))
            {
                // Note that the RootPath is currently case sensitive and must be forward slashes e.g. "c:/temp"
                // and must not end with a slash, otherwise cuts off first char of filename
                // This is scheduled for fix in next release
                tarArchive.RootPath = sourceDirectory.Replace('\\', '/');
                if (tarArchive.RootPath.EndsWith("/"))
                    tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);

                AddDirectoryFilesToTar(tarArchive, sourceDirectory, true, token);
            }
        }

        private void AddDirectoryFilesToTar(TarArchive tarArchive, string sourceDirectory, bool recurse, CancellationToken token)
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
                    AddDirectoryFilesToTar(tarArchive, directory, recurse, token);
            }
        }
    }
}
