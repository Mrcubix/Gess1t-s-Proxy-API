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
        private ConnectionHandler connectionHandler;
        public HttpListener server;
        int port;
        int socketPort;
        string path = Path.Combine(Directory.GetCurrentDirectory(),"Overlays");
        public HTTPServer(ConnectionHandler connectionHandler, int port, int socketPort)
        {
            this.connectionHandler = connectionHandler;
            this.port = port;
            this.socketPort = socketPort;
        }
        public void Start()
        {
            server = new HttpListener();
            try
            {
                server.Prefixes.Add($"http://localhost:{port}/");
                server.Start();
                Console.WriteLine($"HTTP Server: Now Running on port {port}");
                Log.Debug("HTTP Server", $"Now Running on port {port}");
            }
            catch(Exception e)
            {
                Log.Write("HTTP Server", e.ToString(), LogLevel.Error);
                Log.Write("HTTP Server", "Port might not be available, choosing a random one...", LogLevel.Error);
                port = GetPort();
                Start();
                return;
            }
                _ = Task.Run(WaitForRequestAsync);
        }
        public void Stop()
        {
            server.Stop();
            server.Close();
        }
        public int GetPort()
        {
            TcpListener tcpserver = new TcpListener(IPAddress.Any, 0);
            tcpserver.Start();
            int port = ((IPEndPoint)tcpserver.LocalEndpoint).Port;
            tcpserver.Stop();
            return port;
        }
        public async Task WaitForRequestAsync()
        {
            while(true)
            {
                var context = await server.GetContextAsync();
                answerHTTPGETRequest(context);
            }
        }
        private void answerHTTPGETRequest(HttpListenerContext client) 
        {
            var response = client.Response;
            var request = client.Request;
            string relativePath = client.Request.RawUrl;
            //Log.Write("HTTPServer", $"Current path = {path}, Path requested = {relativePath}");
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
            if (relativePath != "/" & relativePath.ToLower() != "/list" & relativePath.ToLower() != "/socketport" & !File.Exists(filePath))
            {
                response.StatusCode = 404;
                response.Close();
                return false;
            }
            return true;
        }
    }
}