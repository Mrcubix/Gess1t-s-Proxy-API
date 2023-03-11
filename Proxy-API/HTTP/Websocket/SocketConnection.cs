using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;

namespace Proxy_API.HTTP.Websocket
{
    public class SocketConnection
    {
        private SocketServer server;

        public int Id { get; set; }
        public string Pipename { get; set; } = null!;
        public Socket Client { get; set; }

        public SocketConnection(int id, Socket client, SocketServer server)
        {
            this.Id = id;
            this.Client = client;
            this.server = server;
        }


        public async Task ProcessClientAsync()
        {
            await UpgradeConnectionAsync();

            _ = MonitorConnectionAsync();

            await ProcessInitialRequestAsync();
        }

        private async Task MonitorConnectionAsync()
        {
            while (Client.Connected)
            {
                await Task.Delay(1000);
            }

            Log.Debug("Socket", $"Connection {Id} has been closed.");

            server.DisposeConnection(this);
        }

        private async Task<byte[]> ReceiveRequestAsync()
        {
            byte[] data = new byte[8192];
            byte[] request = null!;

            using (MemoryStream ms = new MemoryStream())
            {
                int byteCount;
                do
                {
                    byteCount = Client.Receive(data);
                    await ms.WriteAsync(data, 0, byteCount);
                }
                while (byteCount == 8192);

                request = ms.ToArray();
            }

            return request;
        }

        private async Task UpgradeConnectionAsync()
        {
            byte[] requestdata;

            try
            {
                requestdata = await ReceiveRequestAsync();
            }
            catch (Exception E)
            {
                Log.Debug("Socket", E.ToString());
                Log.Debug("Socket", "Failed to receive data, closing connection...");

                server.DisposeConnection(this);
                return;
            }

            string request = Encoding.UTF8.GetString(requestdata);

            Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                                                   + "Connection: Upgrade" + Environment.NewLine
                                                   + "Upgrade: websocket" + Environment.NewLine
                                                   + "Sec-WebSocket-Accept: " + Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(new Regex("Sec-WebSocket-Key: (.*)").Match(request).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))) + Environment.NewLine + Environment.NewLine);

            try
            {
                Client.Send(response);
            }
            catch (Exception E)
            {
                Log.Debug("Socket", E.ToString());
                Log.Debug("Socket", "Failed to send connection upgrade to websocket, closing connection...");

                server.DisposeConnection(this);
            }
        }

        /// <summary>
        ///   Process the initial request from the client.<br/>
        ///   The first request of a connection should contain the identifier of the plugin pipe <br/>
        ///   it's trying to get data from.
        /// </summary>
        private async Task ProcessInitialRequestAsync()
        {
            byte[] requestbytes;

            try
            {
                // receive initial request
                requestbytes = await ReceiveRequestAsync();
            }
            catch (Exception E)
            {
                Log.Debug("Socket", E.ToString());
                Log.Debug("Socket", "Failed to receive data, closing connection...");
                server.DisposeConnection(this);
                return;
            }

            string requestString = WebSocketEncoding.DecodeEncodedString(requestbytes, this);
            Log.Debug("Socket", $"{requestString}");

            var request = JsonSerializer.Deserialize<Dictionary<string, string>>(requestString);

            if (request == null)
            {
                Log.Debug("Socket", "Failed to deserialize request, closing connection...");
                server.DisposeConnection(this);
                return;
            }

            // check if request contains an identifier, which is the name of the plugin pipe
            if (request.ContainsKey("id"))
            {
                if (!string.IsNullOrWhiteSpace(request["id"]))
                {
                    Pipename = request["id"];
                }
                else
                {
                    Log.Debug("Socket", "Incorrect pipename, closing connection...");
                    server.DisposeConnection(this);
                }
            }
        }

        /// <summary>
        ///   Handle further requests from the client.
        ///   Request should contain the method name and optional parameters.
        /// </summary>
        public async Task HandleFurtherRequestsAsync()
        {
            while (true)
            {
                byte[] requestbytes;

                try
                {
                    requestbytes = await ReceiveRequestAsync();
                }
                catch (Exception E)
                {
                    Log.Debug("Socket", E.ToString());
                    Log.Debug("Socket", "Failed to receive data, closing connection...");

                    server.DisposeConnection(this);
                    return;
                }

                string requestString = WebSocketEncoding.DecodeEncodedString(requestbytes, this);
                Log.Debug("Socket", $"{requestString}");

                var request = JsonSerializer.Deserialize<Dictionary<string, string>>(requestString);

                if (request == null)
                    return;

                if (request.ContainsKey("method"))
                {
                    var method = request["method"];

                    if (string.IsNullOrWhiteSpace(request["method"]))
                    {
                        return;
                    }

                    string[]? parameters = null;

                    if (request.ContainsKey("parameters"))
                    {
                        var serializedParameters = request["parameters"];
                        parameters = JsonSerializer.Deserialize<string[]>(serializedParameters);
                    }

                    _ = server.InvokeAsync(Pipename, request["method"], parameters);
                }
            }
        }

        public void Send(string data)
        {
            Client.Send(WebSocketEncoding.EncodeDecodedString(data));
        }

        public void CloseConnection()
        {
            Client.Disconnect(false);
            Client.Dispose();
        }
    }
}