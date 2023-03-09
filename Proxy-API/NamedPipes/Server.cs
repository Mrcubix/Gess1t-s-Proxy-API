using System.IO.Pipes;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using Proxy_API.HTTP.Websocket;
using StreamJsonRpc;

namespace Proxy_API.NamedPipes
{
    public class Server
    {
        public string pipename;
        public SocketServer socketServer;
        private JsonRpc rpc = null!;
        private bool running = true;


        public Server(string pipename, SocketServer socketServer) 
        {
            this.pipename = pipename;
            this.socketServer = socketServer;
        }

        
        public async Task StartAsync()
        {
            while (running)
            {
                NamedPipeServerStream server = new NamedPipeServerStream(pipename, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                Log.Debug($"{pipename}", "Server Pipe: Waiting for connection...");
                await server.WaitForConnectionAsync();
                _ = Task.Run(async () => {
                    Log.Debug($"{pipename}", "Server Pipe: Connected");
                    rpc = JsonRpc.Attach(server, socketServer);
                    Log.Debug($"{pipename}", "Server Pipe: Listening to request...");
                    await rpc.Completion;
                    Log.Debug($"{pipename}", "Server Pipe: Client Disconnected, Disposing and restarting...");
                    rpc.Dispose();
                    await server.DisposeAsync();
                });
            }
        }
    }
}