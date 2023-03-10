using System;
using System.Threading;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using Proxy_API.HTTP;
using Proxy_API.HTTP.Websocket;
using Proxy_API.NamedPipes;
using Proxy_API.Lib.Overlay.Extraction;
using System.Reflection;

namespace Proxy_API
{
    [PluginName("Gess1t's Proxy API")]
    public class Proxy_API : ITool
    {
        private static readonly ManualResetEvent resetEvent = new ManualResetEvent(false);

        SocketServer socketServer = null!;
        HTTPServer httpServer = null!;
        Server server = null!;

        string zipEmbeddedResource = $"Proxy_API.res.overlays.zip";

#region Initialization

        public bool Initialize()
        {
            _ = Task.Run(InitializeAsync);
            return true;
        }

        public async Task InitializeAsync()
        {
            Log.Debug("Location", $"Extracting overlays if neccessary...");

            // check if overlays have been extracted, if not, extract them
            if (!ExtractOverlays())
            {
                Log.Write("Location", $"Overlays could not be extracted", LogLevel.Error);
                return;
            }

            Log.Debug("Location", $"Starting servers...");

            socketServer = new SocketServer(_socketPort, Retries);
            await socketServer.StartAsync();

            httpServer = new HTTPServer(_HTTPPort, _socketPort);
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
            if (OverlayExtractor.AssemblyHasAlreadyBeenExtracted(zipEmbeddedResource))
            {
                Log.Write("Location", $"Overlays have already been extracted", LogLevel.Info);
                return true;
            }

            try
            {
                return OverlayExtractor.TryExtractingEmbeddedResource(Assembly.GetExecutingAssembly(), zipEmbeddedResource, OverlayExtractor.overlayDirectory);
            }
            catch (Exception e)
            {
                Log.Write("Location", $"Exception while trying to extract overlays: {e}", LogLevel.Error);
                return false;
            }
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
            get => _socketPort;
            set
            {
                _socketPort = Math.Min(value, 65535);
            }
        }
        private int _socketPort;

        [Property("Plugin Connections Retries"),
         Unit("Retries"),
         DefaultPropertyValue(3),
         ToolTip("Proxy API:\n\n" +
                 "The number of times a client will attempt to connect to a specified plugin before failing.")
        ]
        public int Retries 
        {
            get => _retries;
            set
            {
                _retries = Math.Min(value, 10);
            }
        }
        private int _retries;
    }

#endregion

}
