using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using Proxy_API.Lib.Overlay.Extraction;

namespace Proxy_API.HTTP
{
    class HTTPServer
    {
        private string[] includedPaths = new string[3] {"/", "/list", "/socketport"};
        private HttpListener listener = null!;

        public int Port {get; set; }
        public int SocketPort { get; set; }
        public bool Disposed { get; set; }
        

        public HTTPServer(int port, int socketPort)
        {
            this.Port = port;
            this.SocketPort = socketPort;
        }

#region Initialization

        public void Start()
        {
            listener = new HttpListener();
            
            StartListening();

            Log.Debug("HTTP Server", $"Now Running on port {Port}");
            
            _ = WaitForConnectionsAsync();
        }

        private void StartListening()
        {
            listener.Prefixes.Add($"http://localhost:{Port}/");

            listener.Start();
        }

        public async Task WaitForConnectionsAsync()
        {
            while (true)
            {
                // there is a known issue with GetContextAsync() where it will just hang for an undertermined amount of time and not provide any requests
                var context = await listener.GetContextAsync();
                answerHTTPGETRequest(context);
            }
        }

#endregion

#region Data Exchange

        private void answerHTTPGETRequest(HttpListenerContext client) 
        {
            var response = client.Response;
            var request = client.Request;

            string? relativePath = client.Request.RawUrl;
            string filePath = Path.Combine(OverlayExtractor.OverlayDirectory, "." + relativePath);

            response.StatusCode = 404;

            if (relativePath == null)
            {
                response.Close();
                return;
            }

            if (!isFileRequestValid(relativePath, filePath))
            {
                response.StatusCode = 403;
                response.Close();
                return;
            }

            byte[] contents = null!;
            switch(relativePath.ToLower())
            {
                case "/":
                    filePath = Path.Combine(OverlayExtractor.OverlayDirectory, "index.html");
                    contents = File.ReadAllBytes(filePath);
                    break;
                case "/list":
                    contents = OverlayListResponse();
                    break;
                case "/socketport":
                    contents = Encoding.UTF8.GetBytes(SocketPort.ToString());
                    break;
                default:
                    contents = File.ReadAllBytes(filePath);
                    break;
            }

            response.StatusCode = 200;
            response.ContentType = request.ContentType;
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = contents.LongLength;
            response.Close(contents, false);
        }

        public byte[] OverlayListResponse()
        {
            DirectoryInfo overlaysDirectory = new DirectoryInfo(OverlayExtractor.OverlayDirectory);

            if (!overlaysDirectory.Exists)
            {
                return Encoding.UTF8.GetBytes("[]");
            }

            IEnumerable<string> overlaysEnumerable = from folder in overlaysDirectory.GetDirectories()
                                                     select folder.Name;

            List<string> overlays = overlaysEnumerable.ToList();

            overlays.RemoveAll(x => x == "js" | x == "css" | x == "img" | x == "source");

            Dictionary<string, List<string>> overlaysJSON = new Dictionary<string, List<string>>();
            overlaysJSON.Add("data", overlays);

            string serializedOverlays = JsonSerializer.Serialize(overlaysJSON);

            return Encoding.UTF8.GetBytes(serializedOverlays);
        }

        public bool isFileRequestValid(string relativePath, string filePath)
        {
            if (!includedPaths.Contains(relativePath.ToLower()) & !File.Exists(filePath))
            {
                return false;
            }

            if (relativePath == "/")
            {
                var indexPath = Path.Combine(OverlayExtractor.OverlayDirectory, "index.html");
                if (!File.Exists(indexPath))
                {
                    return false;
                }
            }

            return true;
        }

#endregion

        public void Stop()
        {
            listener.Stop();
            listener.Close();
        }
    }
}