using System;
using System.Threading;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using Proxy_API.HTTP;
using Proxy_API.NamedPipes;
using Proxy_API.Lib.Overlay.Extraction;

namespace Proxy_API
{
    [PluginName("Gess1t's Proxy API")]
    public class Proxy_API : ITool
    {
        private static readonly ManualResetEvent resetEvent = new ManualResetEvent(false);

        SocketServer socketServer = null!;
        HTTPServer httpServer = null!;
        Server server = null!;

        private bool firstUse = false;

#region Initialization

        public bool Initialize()
        {
            _ = Task.Run(InitializeAsync);
            return true;
        }

        public async Task InitializeAsync()
        {

            // check if overlays have been extracted, if not, extract them
            if (!ExtractOverlays())
            {
                Log.Write("Location", $"Overlays could not be extracted", LogLevel.Error);
                return;
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
            socketServer = null!;
            httpServer.Stop();
            httpServer = null!;
            server = null!;
        }

        public bool ExtractOverlays()
        {
            if (OverlayExtractor.AssemblyHasAlreadyBeenExtracted())
            {
                Log.Write("Location", $"Overlays are missing, extracting now...", LogLevel.Info);
                return true;
            }

            return OverlayExtractor.TryExtractingEmbeddedResource("Location.res.overlays.zip", OverlayExtractor.overlayDirectory);
        }

#endregion

#region Properties

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
    }

#endregion

}
