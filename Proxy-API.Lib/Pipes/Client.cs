using System.IO.Pipes;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using StreamJsonRpc;

namespace Proxy_API.Lib.Pipes
{
    /// <summary>
    /// Represents a client that connects to a named pipe server.
    /// </summary>
    public class Client
    {
        public string Pipename { get; set; }
        public JsonRpc Rpc { get; set; }= null!;
        private NamedPipeClientStream client = null!;
        public bool IsConnected { get; set; }= false;


        public Client(string pipename)
        {
            this.Pipename = pipename;
        }


        public async Task StartConnectionAsync()
        {
            client = new NamedPipeClientStream(".", Pipename, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough | PipeOptions.CurrentUserOnly);
            
            Log.Debug("Proxy API Client", "Connecting...");
            await client.ConnectAsync();

            IsConnected = true;
            Log.Debug("Proxy API Client", "Connected.");

            Rpc = JsonRpc.Attach(client);
            Rpc.Disconnected += (_, _) =>
            {
                IsConnected = false;
                client.Dispose();
                Rpc.Dispose();
            };
            
            Log.Debug("Proxy API Client", $"Client (tool) now listen to {Pipename}");
        }

        public async Task<object> CallRemoteMethodAsync(string methodname)
        {
            return await Rpc.InvokeAsync<object>(methodname);
        }
        
        public void Dispose()
        {
            client.Dispose();
            Rpc.Dispose();
        }
    }
}
