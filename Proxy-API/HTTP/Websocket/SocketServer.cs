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
        private List<PluginConnection> pluginConnections = new List<PluginConnection>();
        private List<SocketConnection> connections = new List<SocketConnection>();
        private static IPAddress addr = IPAddress.Parse("127.0.0.1");
        private IPEndPoint endpoint = null!;
        private Socket server = null!;
        private int maxRetries = 0;

        public int port { get; set; }
        public bool Disposed = false;


        public SocketServer(int port, int retries)
        {
            this.port = port;
            this.maxRetries = retries;
        }

#region Initialization

        public void Start()
        {
            server = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            StartListening();

            Log.Debug("Socket", $"Now listening on port {port}");

            _ = WaitForConnectionAsync();
        }

        private void StartListening()
        {
            endpoint = new IPEndPoint(addr, port);

            // Attempt at binding to a specified port
            server.Bind(endpoint);
            // Start listening for incoming connections
            server.Listen();
        }

        private async Task WaitForConnectionAsync()
        {
            while (!Disposed)
            {
                // Accept an incoming connection
                Socket client = await server.AcceptAsync();

                Log.Debug("Socket", "A Client has connected, starting a new thread...");

                connections.Add(new SocketConnection(connections.Count, client, this));
                _ = Task.Run(() => connections.Last().ProcessClientAsync());
            }
        }

#endregion

#region Data Exchange

        public async Task SendDataAsync(string pipename, string dataIdentifier, object data)
        {
            IEnumerable<SocketConnection> ConnectionsEnumerable = from connection in connections
                                                                  where connection.Pipename == pipename
                                                                  select connection;

            foreach (SocketConnection connection in ConnectionsEnumerable)
            {
                string? serializeData = data.ToString();
                await Task.Run(() => connection.Send("{\"" + dataIdentifier + "\":" + serializeData + "}"));
            }
        }

        public async Task InvokeAsync(string pipename, string method, string[]? parameters)
        {
            PluginConnection? pluginConnection = await GetPluginConnectionAsync(pipename);

            if (pluginConnection == null)
            {
                Log.Debug("Socket", "Failed to connect to plugin, aborting...");
                return;
            }

            await pluginConnection.InvokeAsync(method, parameters);
        }

#endregion

        public async Task<PluginConnection?> GetPluginConnectionAsync(string pipename, int retries = 0)
        {
            PluginConnection? pluginConnection = pluginConnections.FirstOrDefault(x => x.Pipename == pipename);

            // if the plugin connection does not exist, create it
            if (pluginConnection == null)
                pluginConnection = new PluginConnection(pipename);

            if (!pluginConnection.IsConnected)
            {
                try
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    cts.CancelAfter(20000);

                    await pluginConnection.StartAsync();

                    cts.Dispose();
                }
                catch (OperationCanceledException)
                {
                    if (retries >= maxRetries)
                    {
                        Log.Debug("Socket", "Failed to connect to plugin, max retries reached.");
                        return null;
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

#region Disposing

        public void DisposeConnection(SocketConnection connection)
        {
            Log.Debug("Socket", "A Client has disconnected, disposing...");
            connection.CloseConnection();
            // I don't really trust Remove and however it compare elements to find the right one
            connections.RemoveAt(connection.Id);
        }

        public void DisposeConnection(PluginConnection connection)
        {
            Log.Debug("Socket", "A Plugin has disconnected, disposing...");
            connection.Dispose();
            pluginConnections.Remove(connection);
        }

        public void Dispose()
        {
            foreach (SocketConnection connection in connections)
                DisposeConnection(connection);

            foreach (PluginConnection connection in pluginConnections)
                DisposeConnection(connection);

            server.Disconnect(false);

            server.Dispose();
            Disposed = true;
        }
    }

#endregion

}
