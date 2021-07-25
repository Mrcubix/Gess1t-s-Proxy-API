using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Proxy_API
{
    public class Server
    {
        public string pipename;
        public SocketServer socketServer;
        private JsonRpc rpc;
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
                Console.WriteLine($"{pipename} Server Pipe: Waiting for connection...");
                await server.WaitForConnectionAsync();
                _ = Task.Run(async () => {
                    Console.WriteLine($"{pipename} Server Pipe: Connected");
                    rpc = JsonRpc.Attach(server, socketServer);
                    Console.WriteLine($"{pipename} Server Pipe: Listening to request...");
                    await rpc.Completion;
                    Console.WriteLine($"{pipename} Server Pipe: Client Disconnected, Disposing and restarting...");
                    rpc.Dispose();
                    await server.DisposeAsync();
                });
            }
        }
    }
}