using System.IO;
using System.IO.Compression;
using System.Reflection;
using OpenTabletDriver.Plugin;

namespace Proxy_API.Lib.Dependencies
{
    public class DependenciesInstaller
    {
        public static bool InstallDepedencies(string group, string resourcePath, string destinationDirectory, bool forceInstall = false)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            var dependencies = assembly.GetManifestResourceStream(resourcePath);

            if (dependencies == null)
            {
                Log.Write($"{group} Installer", "Failed to open embedded dependencies.", LogLevel.Error);
                return false;
            }

            int entriesCount = 0;
            int installed = 0;

            using (ZipArchive archive = new ZipArchive(dependencies, ZipArchiveMode.Read))
            {
                var entries = archive.Entries;
                entriesCount = entries.Count;

                foreach (ZipArchiveEntry entry in entries)
                {
                    string destinationPath = $"{destinationDirectory}/{entry.FullName}";

                    if (File.Exists(destinationPath) && !forceInstall)
                        continue;

                    entry.ExtractToFile(destinationPath, true);
                    installed++;
                }
            }

            if (installed > 0)
                Log.Write($"{group} Installer", $"Installed {installed} of {entriesCount} dependencies.", LogLevel.Info);

            return true;
        }
    }
}