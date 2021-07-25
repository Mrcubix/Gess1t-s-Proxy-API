using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;

namespace Proxy_API
{
    public class SocketConnection
    {
        public Socket connection;
        private SocketServer socketServer;
        public string identifier;
        public SocketConnection(Socket connection, SocketServer socketServer)
        {
            this.connection = connection;
            this.socketServer = socketServer;
        }
        public async Task ProcessClientAsync()
        {
            await UpgradeConnectionAsync();
            _ = Task.Run(async () => 
            {
                while(true)
                {
                    if (!connection.Connected)
                    {
                        socketServer.DisposeConnection(this);
                        break;
                    }
                    await Task.Delay(1000);
                }
            });
            await ProcessInitialRequestAsync();
        }
        private async Task<byte[]> ReceiveRequestAsync()
        {
            byte[] data = new byte[8192];
            byte[] request = null;
            using (MemoryStream ms = new MemoryStream())
            {
                int byteCount;
                do
                {
                    byteCount = connection.Receive(data);
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
            catch(Exception E)
            {
                Log.Debug("Socket", E.ToString());
                Log.Debug("Socket", "Failed to receive data, closing connection...");
                CloseConnection();
                return;
            }
            string request = BytesToString(requestdata);
            Byte[] response = Encoding.UTF8.GetBytes( "HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                                                    + "Connection: Upgrade" + Environment.NewLine
                                                    + "Upgrade: websocket" + Environment.NewLine
                                                    + "Sec-WebSocket-Accept: " + Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(new Regex("Sec-WebSocket-Key: (.*)").Match(request).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"))) + Environment.NewLine+ Environment.NewLine);
            try
            {
                connection.Send(response);
            }
            catch(Exception E)
            {
                Log.Debug("Socket", E.ToString());
                Log.Debug("Socket", "Failed to send connection upgrade to websocket, closing connection...");
                CloseConnection();
            }
        }
        private async Task ProcessInitialRequestAsync()
        {
            byte[] requestbytes;
            try
            {
                requestbytes = await ReceiveRequestAsync();
            }
            catch(Exception E)
            {
                Log.Debug("Socket", E.ToString());
                Log.Debug("Socket", "Failed to receive data, closing connection...");
                CloseConnection();
                return;
            }
            string requestString = DecodeEncodedString(requestbytes);
            Log.Debug("Socket", $"{requestString}");
            var request = JsonSerializer.Deserialize<Dictionary<string, string>>(requestString);
            if (request.ContainsKey("id"))
            if (!string.IsNullOrWhiteSpace(request["id"]))
            {
                identifier = request["id"];
            }
            else
            {
                Log.Debug("Socket", "Incorrect pipename, closing connection...");
                CloseConnection();
            }
        }
        /*
            TODO
                - Add support for parameters
        */
        public async Task HandleFurtherRequestsAsync()
        {
            while(true)
            {
                byte[] requestbytes;
                try
                {
                    requestbytes = await ReceiveRequestAsync();
                }
                catch(Exception E)
                {
                    Log.Debug("Socket", E.ToString());
                    Log.Debug("Socket", "Failed to receive data, closing connection...");
                    CloseConnection();
                    return;
                }
                string requestString = DecodeEncodedString(requestbytes);
                Log.Debug("Socket", $"{requestString}");
                var request = JsonSerializer.Deserialize<Dictionary<string, string>>(requestString);
                if (request.ContainsKey("method"))
                if (!string.IsNullOrWhiteSpace(request["method"]))
                {
                    await socketServer.NotifyAsync(identifier, request["method"]);
                }
                else
                {
                    Log.Debug("Socket", "Incorrect request, closing connection...");
                    CloseConnection();
                }
            }
        }
        public void Send(string data)
        {
            connection.Send(EncodeDecodedString(data));
        }
        public string DecodeEncodedString(byte[] data)
        {
            if (data[0] == 129)
            {
                if (data[1] > 127)
                {
                    int length = data[1] - 128;
                    int offset = 2;    
                    if (length == 126)
                    {
                        length = BitConverter.ToUInt16(data[2..4]);
                        offset = 4;
                    }
                    if (length == 127)
                    {
                        length = BitConverter.ToUInt16(data[2..10]);
                        offset = 10;
                    } 
                    // YEP first copy pasta for the decoding part https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_server#decoding_algorithm adapted to use slices like previously
                    byte[] decodedData = new byte[length];
                    byte[] mask = data[offset..(offset + 4)];
                    offset += 4;
                    for (int i = 0; i < length; ++i)
                    {
                        decodedData[i] = (byte)(data[offset + i] ^ mask[i % 4]);
                    }
                    return BytesToString(decodedData);
                }
                else
                {
                    CloseConnection();
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public byte[] EncodeDecodedString(string response)
        {
            List<byte> data = new List<byte>();
            data.Add(129);
            if (response.Length < 126)
            {
                data.Add((byte)(response.Length));
            }
            if (response.Length > 125 & response.Length < 65536)
            {
                data.Add(126);
                ushort midlength = Convert.ToUInt16(response.Length);
                data.AddRange(BitConverter.GetBytes(midlength).Reverse());
            }
            if (response.Length > 65535)
            {
                data.Add(127);
                ulong longlength = Convert.ToUInt64(response.Length);
                data.AddRange(BitConverter.GetBytes(longlength).Reverse());
            }
            data.AddRange(Encoding.UTF8.GetBytes(response));
            return data.ToArray();
        }
        public string BytesToString(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }
        public void CloseConnection()
        {
            connection.Disconnect(false);
            connection.Dispose();
        }
    }
}