using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using OpenTabletDriver.Plugin;
using StreamJsonRpc;

namespace Proxy_API.NamedPipes
{
    public class PluginConnection
    {
        private NamedPipeClientStream client = null!;

        public string Pipename { get; set; }
        public bool IsConnected => client != null && client.IsConnected;
        public JsonRpc rpc = null!;

        public PluginConnection(string pipename)
        {
            this.Pipename = pipename;
        }
        
        public async Task StartAsync()
        {
            client = new NamedPipeClientStream(".", Pipename, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough | PipeOptions.CurrentUserOnly);

            await client.ConnectAsync();
            Log.Debug("API", $"Connected to {Pipename}");

            rpc = JsonRpc.Attach(client);
            rpc.Disconnected += (_, _) =>
            {
                Log.Debug("API", $"Disconnected from {Pipename}");
                client.Dispose();
                rpc.Dispose();
            };

            Log.Debug("API:", $"Now listening to {Pipename}");
        }

        public void Dispose()
        {
            client.Dispose();
            rpc.Dispose();
        }

        public async Task InvokeAsync(string method, params object?[]? args)
        {
            await rpc.InvokeAsync(method, args);
        }
    }
}