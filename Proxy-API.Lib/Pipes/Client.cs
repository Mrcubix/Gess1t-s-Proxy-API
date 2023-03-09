using System.IO.Pipes;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using StreamJsonRpc;

namespace Proxy_API.Pipes.Lib
{
    /// <summary>
    /// Represents a client that connects to a named pipe server.
    /// </summary>
    class Client
    {
        public string pipename;
        private JsonRpc rpc = null!;
        public NamedPipeClientStream client = null!;
        public bool isconnected = false;


        public Client(string pipename)
        {
            this.pipename = pipename;
        }


        public async Task StartConnectionAsync()
        {
            client = new NamedPipeClientStream(".", pipename, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough | PipeOptions.CurrentUserOnly);
            
            Log.Debug("Proxy API Client", "Connecting...");
            await client.ConnectAsync();

            isconnected = true;
            Log.Debug("Proxy API Client", "Connected.");

            rpc = JsonRpc.Attach(client);
            rpc.Disconnected += (_, _) =>
            {
                isconnected = false;
                client.Dispose();
                rpc.Dispose();
            };
            
            Log.Debug("Proxy API Client", $"Client (tool) now listen to {pipename}");
        }

        public async Task<object> CallRemoteMethodAsync(string methodname)
        {
            return await rpc.InvokeAsync<object>(methodname);
        }
        
        public void Dispose()
        {
            client.Dispose();
            rpc.Dispose();
        }
    }
}
