using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;

namespace Proxy_API
{
    class HTTPServer
    {
        public int port;
        public int socketPort;
        public string[] includedPaths = new string[3] {"/", "/list", "/socketport"};
        HttpListener listener;
        string path = Path.Combine(Directory.GetCurrentDirectory(),"Overlays");
        public HTTPServer(int port, int socketPort)
        {
            this.port = port;
            this.socketPort = socketPort;
        }
        public async Task StartAsync()
        {
            listener = new HttpListener();
            while(true)
            {
                listener.Prefixes.Add($"http://localhost:{port}/");
                try
                {
                    listener.Start();
                    break;
                }
                catch(Exception E)
                {
                    Log.Debug("HTTP Server", E.ToString());
                    Console.WriteLine("HTTP Server: Listening failed, retrying in 5 seconds with another port...");
                    await Task.Delay(5000);

                    port = GetPort();
                }
            }
            Log.Debug("HTTP Server", $"Now Running on port {port}");
            _ = Task.Run(async () => 
            {
                while(true)
                {
                    var context = await listener.GetContextAsync();
                    answerHTTPGETRequest(context);
                }
            });
        }
        private void answerHTTPGETRequest(HttpListenerContext client) 
        {
            var response = client.Response;
            var request = client.Request;

            string relativePath = client.Request.RawUrl;
            string filePath = Path.Combine(path, "." + relativePath);

            if (!isFileRequestValid(relativePath, filePath, response))
            {
                return;
            }

            byte[] contents = null;
            switch(relativePath.ToLower())
            {
                case "/":
                    filePath = Path.Combine(path, "index.html");
                    contents = File.ReadAllBytes(filePath);
                    break;
                case "/list":
                    contents = OverlayListResponse(response);
                    break;
                case "/socketport":
                    contents = Encoding.UTF8.GetBytes(socketPort.ToString());
                    break;
                default:
                    contents = File.ReadAllBytes(filePath);
                    break;
            }
            response.ContentType = request.ContentType;
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = contents.LongLength;
            response.Close(contents, true);
        }
        public byte[] OverlayListResponse(HttpListenerResponse response)
        {
            IEnumerable<string> overlaysEnumerable = from folder in new DirectoryInfo(path).GetDirectories()
                                                     select folder.Name;
            List<string> overlays = overlaysEnumerable.ToList();
            overlays.RemoveAll(x => x == "js" | x == "css" | x == "img");
            Dictionary<string, List<string>> overlaysJSON = new Dictionary<string, List<string>>();
            overlaysJSON.Add("data", overlays);
            string serializedOverlays = JsonSerializer.Serialize(overlaysJSON);
            return Encoding.UTF8.GetBytes(serializedOverlays);
        }
        public bool isFileRequestValid(string relativePath, string filePath, HttpListenerResponse response)
        {
            if (!includedPaths.Contains(relativePath.ToLower()) & !File.Exists(filePath))
            {
                response.StatusCode = 404;
                response.Close();
                return false;
            }
            return true;
        }
        public int GetPort()
        {
            TcpListener tcpserver = new TcpListener(IPAddress.Any, 0);
            tcpserver.Start();
            int port = ((IPEndPoint)tcpserver.LocalEndpoint).Port;
            tcpserver.Stop();
            return port;
        }
        public void Stop()
        {
            listener.Stop();
            listener.Close();
        }
    }
}