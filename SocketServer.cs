using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;

namespace Proxy_API
{
    public class SocketServer
    {
        public int port;
        List<PluginConnection> pluginConnections = new List<PluginConnection>();
        List<SocketConnection> connections = new List<SocketConnection>();
        static IPAddress addr = IPAddress.Parse("127.0.0.1");
        IPEndPoint endpoint;
        Socket server;
        public SocketServer(int port)
        {
            this.port = port;
        }
        public async Task StartAsync()
        {
            server = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            while(true)
            {
                endpoint = new IPEndPoint(addr, port);
                try
                {
                    server.Bind(endpoint);
                    server.Listen();
                    break;
                }
                catch(Exception E)
                {
                    Console.WriteLine(E);
                    Console.WriteLine("Socket: Listening failed, retrying in 5 seconds...");
                    await Task.Delay(5000);

                    port = GetPort();
                }
            }
            Console.WriteLine($"Socket: Now listening on port {port}");
            _ = Task.Run(async () => 
            {
                while(true)
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
                string responseString = data.ToString();
                await Task.Run(() => connection.Send("{\""+ dataIdentifier + "\":" + responseString + "}"));     
            }
        }
        /*
            TODO:
                - Add support for parameters
        */
        public async Task NotifyAsync(string pipename, string method)
        {
            PluginConnection pluginConnection = await GetPluginConnectionAsync(pipename);
            await pluginConnection.rpc.NotifyAsync(method);
        }
        public async Task<PluginConnection> GetPluginConnectionAsync(string pipename)
        {
            PluginConnection pluginConnection = pluginConnections.Find(x => x.pipename == pipename);
            if (pluginConnection == null)
            {
                pluginConnection = new PluginConnection(pipename);
            }
            if (!pluginConnection.client.IsConnected)
            {
                await pluginConnection.StartAsync();
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
            Console.WriteLine($"Socket: A Client has disconnected, disposing...");
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
