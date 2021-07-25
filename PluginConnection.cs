using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Proxy_API
{
    public class PluginConnection
    {
        public string pipename;
        public NamedPipeClientStream client;
        public JsonRpc rpc;
        public PluginConnection(string pipename)
        {
            this.pipename = pipename;
        }
        public async Task StartAsync()
        {
            client = new NamedPipeClientStream(".", pipename, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough | PipeOptions.CurrentUserOnly);
            await client.ConnectAsync();
            Console.WriteLine($"API: Connected = {client.IsConnected}");
            rpc = JsonRpc.Attach(client);

            rpc.Disconnected += (_, _) =>
            {
                Console.WriteLine($"API: Connected = {client.IsConnected}");
                client.Dispose();
                rpc.Dispose();
            };

            Console.WriteLine($"API: Now listening to {pipename}");
        }
        public void Dispose()
        {
            client.Dispose();
            rpc.Dispose();
        }
    }
}