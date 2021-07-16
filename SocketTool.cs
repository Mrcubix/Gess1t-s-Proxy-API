using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;

namespace Proxy_API
{
    class SocketTool
    {
        ConnectionHandler connectionHandler;
        List<SocketConnection> socketConnectionsInstances = new List<SocketConnection>();
        int port;
        static IPAddress addr = Dns.GetHostEntry("localhost").AddressList[0];
        IPEndPoint localEndpoint = new IPEndPoint(addr, 7272);
        Socket server = new Socket(addr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        Socket clientSocket;
        public SocketTool(ConnectionHandler connectionHandler, int port)
        {
            this.connectionHandler = connectionHandler;
            this.port = port;
        }
        public async Task StartAsync()
        {
            while (true)
            {
                try
                {
                    server.Bind(localEndpoint);
                    server.Listen();
                    while(true)
                    {
                        Log.Debug("Socket", "Waiting for connections...");
                        clientSocket = await server.AcceptAsync();
                        Log.Debug("Socket", "New Client connected");
                        socketConnectionsInstances.Add(new SocketConnection(clientSocket, connectionHandler));
                        _ = Task.Run(socketConnectionsInstances[socketConnectionsInstances.Count - 1].StartHandlingSocketConnectionAsync);
                    }
                }
                catch(Exception e)
                {
                    await Task.Delay(5000);
                    Log.Write("Socket", e.ToString(), LogLevel.Error);
                    Log.Write("Socket", "Port might not be available, choosing a random one...", LogLevel.Error);
                    port = GetPort();
                    await StartAsync();
                    return;
                }
            }
        }
        public int GetPort()
        {
            TcpListener tcpserver = new TcpListener(IPAddress.Any, 0);
            tcpserver.Start();
            int port = ((IPEndPoint)tcpserver.LocalEndpoint).Port;
            tcpserver.Stop();
            return port;
        }
        public void Dispose()
        {
            for (int i = 0; i != socketConnectionsInstances.Count; i++)
            {
                socketConnectionsInstances[i].CloseConnection();
                socketConnectionsInstances[i] = null;
            }
        }
    }
}