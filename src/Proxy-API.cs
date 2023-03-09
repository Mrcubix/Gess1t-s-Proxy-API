using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;

namespace Proxy_API
{
    [PluginName("Gess1t's Proxy API")]
    public class Proxy_API : ITool
    {
        private static readonly ManualResetEvent resetEvent = new ManualResetEvent(false);
        SocketServer socketServer;
        HTTPServer httpServer;
        Server server;
        private string overlayDir = null;
        private bool firstUse = false;
        public Proxy_API()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                overlayDir = Path.Combine(Directory.GetCurrentDirectory(), "Overlays");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                overlayDir = Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".config/OpenTabletDriver/Overlays");
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                overlayDir = Path.Combine(Environment.GetEnvironmentVariable("HOME"), "/Library/Application Support/OpenTabletDriver/Overlays");
            }
            if (!Directory.Exists(overlayDir))
            {
                Directory.CreateDirectory(overlayDir);
                firstUse = true;
            }
            string indexpath = Path.Combine(overlayDir, "index.html");
            if (!File.Exists(indexpath))
            {
                firstUse = true;
            }
        }
        public bool Initialize()
        {
            _ = Task.Run(InitializeAsync);
            return true;
        }
        public async Task InitializeAsync()
        {
            if (firstUse)
            {
                new Thread(new ThreadStart(CopyFiles)).Start();
            }
            socketServer = new SocketServer(_SocketPort);
            await socketServer.StartAsync();
            httpServer = new HTTPServer(_HTTPPort, _SocketPort);
            await httpServer.StartAsync();
            server = new Server("API", socketServer);
            await server.StartAsync();
        }
        public void Dispose()
        {
            socketServer.Dispose();
            socketServer = null;
            httpServer.Stop();
            httpServer = null;
            server = null;
        }
        public void CopyFiles()
        {
            DirectoryInfo source = new DirectoryInfo(Path.Combine(_pluginsPath, "ProxyAPI"));
            foreach (FileInfo file in source.GetFiles())
            {
                file.CopyTo(Path.Combine(overlayDir, file.Name), true);
            }
            foreach(DirectoryInfo directory in source.GetDirectories())
            {
                string targetFolder = Path.Combine(overlayDir, directory.Name);
                Directory.CreateDirectory(targetFolder);
                DirectoryInfo directoryTarget = new DirectoryInfo(targetFolder);
                foreach (FileInfo file in directory.GetFiles())
                {
                    file.CopyTo(Path.Combine(targetFolder, file.Name), true);
                }
            }
        }
        [Property("HTTP Server Port"),
         Unit("Port"),
         DefaultPropertyValue(27272),
         ToolTip("Proxy API:\n\n" +
                 "Port used to start and connect the HTTP server.")
        ]
        public int HTTPPort 
        {
            get => _HTTPPort;
            set
            {
                _HTTPPort = Math.Min(value, 65535);
            }
        }
        private int _HTTPPort;
        [Property("Socket Server Port"),
         Unit("Port"),
         DefaultPropertyValue(7272),
         ToolTip("Proxy API:\n\n" +
                 "Port used to start and connect the Socket server from the overlay.")
        ]
        public int SocketPort 
        {
            get => _SocketPort;
            set
            {
                _SocketPort = Math.Min(value, 65535);
            }
        }
        private int _SocketPort;
        [Property("Plugin folder path"),
         ToolTip("Proxy API:\n\n" +
                 "Folder where this plugin folder is located in.\n\n" +
                 "E.g: 'C:\\Users\\{user}\\AppData\\Local\\OpenTabletDriver\\Plugins\\Proxy API' on windows.")
        ]
        public string pluginsPath 
        {
            get => @_pluginsPath; 
            set
            {
                _pluginsPath = @value;
            }
        }
        public string _pluginsPath;
    }
}
