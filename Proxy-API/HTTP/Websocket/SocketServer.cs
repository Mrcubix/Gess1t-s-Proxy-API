using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using Proxy_API.NamedPipes;

namespace Proxy_API.HTTP.Websocket
{
    public class SocketServer
    {
        private int maxRetries = 0;
        public int port { get; set; }

        private List<PluginConnection> pluginConnections = new List<PluginConnection>();
        private List<SocketConnection> connections = new List<SocketConnection>();
        private static IPAddress addr = IPAddress.Parse("127.0.0.1");
        private IPEndPoint endpoint = null!;
        private Socket server = null!;


        public SocketServer(int port, int retries)
        {
            this.port = port;
            this.maxRetries = retries;
        }


        public async Task StartAsync()
        {
            server = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            while (true)
            {
                endpoint = new IPEndPoint(addr, port);

                try
                {
                    server.Bind(endpoint);
                    server.Listen();
                    break;
                }
                catch (Exception E)
                {
                    Log.Debug("Socket", E.ToString());
                    Log.Debug("Socket", "Listening failed, retrying in 5 seconds...");
                    await Task.Delay(5000);

                    port = GetPort();
                }
            }

            Log.Debug("Socket", $"Now listening on port {port}");

            _ = Task.Run(async () =>
            {
                while (true)
                {
                    Socket client = await server.AcceptAsync();
                    connections.Add(new SocketConnection(client, this));
                    _ = Task.Run(() => connections[connections.Count - 1].ProcessClientAsync());
                }
            });
        }

        public async Task SendDataAsync(string identifier, string dataIdentifier, object data)
        {
            IEnumerable<SocketConnection> ConnectionsEnumerable = from connection in connections
                                                                  where connection.identifier == identifier
                                                                  select connection;
            foreach (SocketConnection connection in ConnectionsEnumerable)
            {
                string? responseString = data.ToString();
                await Task.Run(() => connection.Send("{\"" + dataIdentifier + "\":" + responseString + "}"));
            }
        }

        public async Task NotifyAsync(string pipename, string method, string[]? parameters)
        {
            PluginConnection? pluginConnection = await GetPluginConnectionAsync(pipename);

            if (pluginConnection == null)
            {
                Log.Debug("Socket", "Failed to connect to plugin, aborting...");
                return;
            }

            await pluginConnection.rpc.NotifyAsync(method, parameters);
        }

        public async Task<PluginConnection?> GetPluginConnectionAsync(string pipename, int retries = 0)
        {
            PluginConnection? pluginConnection = pluginConnections.FirstOrDefault(x => x.pipename == pipename);

            if (pluginConnection == null)
            {
                pluginConnection = new PluginConnection(pipename);
            }
            if (!pluginConnection.client.IsConnected)
            {
                try
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.CancelAfter(20000);

                    await pluginConnection.StartAsync();
                }
                catch (OperationCanceledException)
                {
                    if (retries >= maxRetries)
                    {
                        Log.Debug("Socket", "Failed to connect to plugin, max retries reached.");
                        return pluginConnection;
                    }
                    else
                    {
                        Log.Debug("Socket", "Failed to connect to plugin, retrying in 5 seconds...");
                        await Task.Delay(5000);
                        await GetPluginConnectionAsync(pipename, retries + 1);
                    }
                }
            }

            return pluginConnection;
        }

        public int GetPort()
        {
            TcpListener tcpserver = new TcpListener(IPAddress.Any, 0);
            tcpserver.Start();
            int port = ((IPEndPoint)tcpserver.LocalEndpoint).Port;
            tcpserver.Stop();
            return port;
        }

        public void DisposeConnection(SocketConnection connection)
        {
            Log.Debug("Socket", "A Client has disconnected, disposing...");
            connection.CloseConnection();
            connections.Remove(connection);
        }

        public void Dispose()
        {
            for (int i = 0; i != connections.Count; i++)
            {
                connections[i].CloseConnection();
                connections.RemoveAt(i);
            }
            for (int i = 0; i != pluginConnections.Count; i++)
            {
                pluginConnections[i].Dispose();
                pluginConnections.RemoveAt(i);
            }
            server.Disconnect(false);
            server.Dispose();
        }
    }
}
