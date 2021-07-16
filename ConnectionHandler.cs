using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace Proxy_API
{
    class ConnectionHandler
    {
        public List<Client> clientInstances = new List<Client>();
        public async Task StartPluginConnectionAsync(string pipename)
        {
            if (!clientInstances.Exists(x => x.pipename == pipename))
            {
                clientInstances.Add(new Client(pipename));
                await clientInstances[clientInstances.Count -1].StartConnectionAsync();
            }
        }
        public async Task ExternalEndpointStartAsync()
        {
            while (true)
            {
                NamedPipeServerStream server = new NamedPipeServerStream("ExternalEndpoint", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync();
                // Process the request and call the appropriate function in the appropriate client instance
            }
        }
        public async Task<object> CallRemotePipeMethodAsync(string pipename, string method)
        {
            if (!clientInstances.Exists(x => x.pipename == pipename))
            {
                await StartPluginConnectionAsync(pipename);
            }
            if (!clientInstances.Find(x => x.pipename == pipename).isconnected)
            {
                await StartPluginConnectionAsync(pipename);
            }
            object result = await clientInstances.Find(x => x.pipename == pipename).CallRemoteMethodAsync(method);
            return result;
        }
        public void Dispose()
        {
            foreach(Client client in clientInstances)
            {
                client.Dispose();
            }
        }
    }
}