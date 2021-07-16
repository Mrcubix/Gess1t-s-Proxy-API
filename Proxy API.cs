using System;
using System.IO;
using System.Threading;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;

namespace Proxy_API
{
    [PluginName("Proxy API")]
    public class Proxy_API : ITool
    {
        private static readonly ManualResetEvent resetEvent = new ManualResetEvent(false);
        ConnectionHandler connectionHandler;
        HTTPServer httpserver;
        SocketTool socket;
        private string overlayDir = null;
        private bool firstUse = false;
        public Proxy_API()
        {
            overlayDir = Path.Combine(Directory.GetCurrentDirectory(), "Overlays");
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
        public static void Main()
        {
            Proxy_API proxy = new Proxy_API();
            proxy.Initialize();
            resetEvent.WaitOne();
        }
        public bool Initialize()
        {
            new Thread(new ThreadStart(InitializeTool)).Start();
            return true;
        }
        public void InitializeTool()
        {
            if (firstUse)
            {
                new Thread(new ThreadStart(CopyFiles)).Start();
            }
            connectionHandler = new ConnectionHandler();
            _SocketPort = 7272;
            socket = new SocketTool(connectionHandler, _SocketPort);
            _ = socket.StartAsync();
            _HTTPPort = 27272;
            httpserver = new HTTPServer(connectionHandler, _HTTPPort, _SocketPort);
            httpserver.Start();
        }
        public void Dispose()
        {
            connectionHandler.Dispose();
            connectionHandler = null;
            httpserver.Stop();
            httpserver = null;
            socket.Dispose();
            socket = null;
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
