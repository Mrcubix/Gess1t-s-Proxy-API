using System.IO.Pipes;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using StreamJsonRpc;

namespace Proxy_API.Lib.Pipes
{
    public class Server
    {
        public string Pipename { get; set; }
        public JsonRpc rpc { get; set; } = null!;
        private NamedPipeServerStream server;
        private object objectToAttach;
        public bool Running { get; set; } = false;


        public Server(string pipename, object objectToAttach) 
        {
            this.Pipename = pipename;
            this.objectToAttach = objectToAttach;
        }


        public async Task StartAsync()
        {
            Running = true;

            while (Running)
            {
                server = new NamedPipeServerStream(Pipename, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                
                Log.Debug($"{Pipename}", "Waiting for connection...");
                await server.WaitForConnectionAsync();

                Log.Debug($"{Pipename}", "Connected");
                rpc = JsonRpc.Attach(server, objectToAttach);

                Log.Debug($"{Pipename}", "Listening to request...");
                await rpc.Completion;

                Log.Debug($"{Pipename}", "Client Disconnected, Disposing and restarting...");
                rpc.Dispose();
                
                await server.DisposeAsync();
            }
        }

        public void Dispose()
        {
            Running = false;
            rpc.Dispose();
            server.Dispose();
        }
    }
}