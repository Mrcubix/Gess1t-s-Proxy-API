using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using OpenTabletDriver.Plugin;
using Proxy_API.Lib.Interop;

namespace Proxy_API.Lib.Overlay.Extraction
{
    /// <summary>
    /// Extracts the embedded, specified overlay file to the overlays directory
    /// </summary>
    public class OverlayExtractor
    {
        #nullable enable
        private static string? homeDirectory = Environment.GetEnvironmentVariable("HOME");
        private static string pluginLocation = Assembly.GetExecutingAssembly().Location;
        private static string currentDirectory = Directory.GetCurrentDirectory();

        public static string overlayDirectory = SystemInterop.CurrentPlatform switch
        {
            PluginPlatform.Windows => $"{currentDirectory}/overlays",
            PluginPlatform.Linux => $"{homeDirectory}/.config/OpenTabletDriver/overlays",
            PluginPlatform.MacOS => $"{homeDirectory}/Library/Application Support/OpenTabletDriver/overlays",
            _ => ""
        };
        public static string overlaySourceDirectory = $"{overlayDirectory}/source";


        public static bool TryExtractingEmbeddedResource(string source, string destinationDirectory)
        {
            if (overlayDirectory == "")
            {
                Log.Write("Location", $"Your platform is unsupported", LogLevel.Error);
                return false;
            }

            var assembly = Assembly.GetExecutingAssembly();

            var resourceName = "Location.res.overlays.zip";

            // get the resource stream
            var resourceStream = assembly.GetManifestResourceStream(source);

            // if the resource stream is null, we couldn't find the resource
            if (resourceStream == null)
            {
                Log.Write("Location", $"Could not find resource '{resourceName}'", LogLevel.Error);
                return false;
            }

            MemoryStream memoryStream = new MemoryStream();
            resourceStream.CopyTo(memoryStream);

            byte[] data = memoryStream.ToArray();

            // create the directory if it doesn't exist
            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            // pass the stream to a zip file reader
            using (var zip = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                // iterate through each file in the zip
                foreach (var entry in zip.Entries)
                {
                    // get the full path to where we want to extract the file to
                    var destinationPath = Path.Combine(destinationDirectory, entry.FullName);

                    // if the file is a directory, create it
                    if (entry.Name == "")
                    {
                        Directory.CreateDirectory(destinationPath);
                        continue;
                    }

                    // extract the file
                    entry.ExtractToFile(destinationPath, true);
                }
            }

            if (!Directory.Exists(overlaySourceDirectory))
                Directory.CreateDirectory(overlaySourceDirectory);

            // finally, write the zip file to disk
            using (var fileStream = new FileStream($"{overlaySourceDirectory}/{source}", FileMode.Create))
            {
                fileStream.Write(data, 0, data.Length);
            }

            return true;
        }

        public static bool AssemblyHasAlreadyBeenExtracted() => File.Exists($"{overlaySourceDirectory}/overlays.zip");
    }
}