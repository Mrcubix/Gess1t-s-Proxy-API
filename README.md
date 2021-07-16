# API for OpenTabletDriver

Known Compatible with OpenTabletDriver 0.5.3.1

## Features:

This plugin will allow you to communicate with plugins implmenting a pipe server and StreamJsonRpc's library.

Head to http://localhost:27272 in your browser to see the list of overlays if you have any installed.  
(Port might differ depending on settings)

## Example object:
```cs
using System.Threading.Tasks;

class ExampleObject
{
    object someObject;

    public async Task<object> SomeTask()
    {
        // You could force a delay of 5 seconds for example by adding
        // Task.Delay(5000);
        // OR
        // Synchronise it with the tablet report using ManualResetEvent
        return someObject;
    }
}
```
## Example Server:
```cs
using System.IO.Pipes;
using System.Threading.Tasks;
using StreamJsonRpc;

class Server
{
    public string pipename = "SomePipeName";
    private NamedPipeServerStream server;
    private JsonRpc rpc;
    private Object objectToAttach;
    private bool running = true;

    public Server(Object objectToAttach)
    {
        this.objectToAttach = objectToAttach;
    }

    public async Task Start()
    {
        while(running)
        {
            server = new NamedPipeServerStream(pipename, PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            await server.WaitForConnectionAsync();
            rpc = JsonRpc.Attach(server, objectToAttach);
            await rpc.Completion;
            rpc.Dispose();
            await server.DisposeAsync();
        }
    }

    public void Dispose()
    {
        running = false;
        rpc.Dispose();
        server.Dispose();
    }
}
```
## Example client (In Browser) (Javascript):

```js
document.addEventListener('DOMContentLoaded', async function () {

    let request = await fetch("/SocketPort");
    let text = await request.text();
    let port = parseInt(text);

    let webSocketURL = "ws://localhost:"+port;
    var socket = new WebSocket(webSocketURL);

    socket.onopen = function (event) {
        // Initial request, in string form
        socket.send('{"pipe": "SomePipeName"}');
    };
    socket.onmessage = function (message) {
        // Receive the response in string form
        var json = JSON.parse(message.data);
        console.log(json);
    }
})
```

The following elements must be taken into account when making a plugin for this API:  
- First, you plugin must contain a public Task called `GetMethods()` that will return a string of the serialized array of method names you want to run periodically.  
(`return JsonSerializer.Serialize(array);` for example)  
- Second, Plugins using this tool are required to expose their public Task to StreamJsonRpc by attaching an instance of the Object implmenting them.  
- Finally, all timing need to be implemented on the plugin side, if you want to add delay between requests for a specific function, you will have to add 
`await Task.Delay(time);` or use a ManualResetEvent.  