# API for OpenTabletDriver

Known Compatible with OpenTabletDriver 0.5.3.1

## Features:

This plugin will allow you to communicate with plugins implmenting a pipe server and StreamJsonRpc's library.

Head to http://localhost:27272 in your browser to see the list of overlays if you have any installed.  
(Port might differ depending on settings)

## Example object:
```cs
using system;
using EventTimer = System.Timers.Timer;


class ExampleObject
{
    public event EventHandler<object> someEvent;
    public EventTimer someTimer = new EventTimer(5000); // define a timer with a 5 second interval
    object someObject;

    // Send data when an event is Invoked
    someEvent += (_,_) =>
    {
        _ = client.rpc.NotifyAsync("SendDataAsync", "PluginIdentifier", "DataIdentifier", someObject);
    }
    // Hook an event to the Elapsed event of the timer and enable it
    public void SomeInitMethod()
    {
        someTimer.Elapsed += (_,_) =>
        {
            _ = client.rpc.NotifyAsync("SendDataAsync", "PluginIdentifier", "DataIdentifier", someObject);
        }
        someTimer.Start();
    }
}
```
## Example Client (Filter / Interpolator):
```cs
using System.IO.Pipes;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Area_Randomizer
{
    public class Client
    {
        public string pipename = "API";
        public NamedPipeClientStream client;
        public JsonRpc rpc;
        public Client(string pipename)
        {
            this.pipename = pipename;
        }
        public async Task StartAsync()
        {
            client = new NamedPipeClientStream(".", pipename, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough | PipeOptions.CurrentUserOnly);
            await client.ConnectAsync();
            rpc = JsonRpc.Attach(client);

            rpc.Disconnected += (_, _) =>
            {
                client.Dispose();
                rpc.Dispose();
                _ = StartAsync();
            };
        }
        public void Dispose()
        {
            client.Dispose();
            rpc.Dispose();
        }
    }
}
```
## Example client (In Browser) (Javascript):

```js
document.addEventListener('DOMContentLoaded', async function () {

    // The Socket port is fecthed using an HTTP request
    let request = await fetch("/SocketPort");
    let text = await request.text();
    let port = parseInt(text);

    let webSocketURL = "ws://localhost:"+port;
    var socket = new WebSocket(webSocketURL);

    socket.onopen = function (event) {
        // Initial request, in string form
        socket.send('{"id": "PluginIdentifier"}');
    };
    socket.onmessage = function (message) {
        // Receive the response in string form
        var json = JSON.parse(message.data);
        console.log(json);
    }
})
```

The following elements must be taken into account when making a plugin for this API:  
- First, your Filter / Interpolator need to notify the Tool in some way (see the example), the notification must follow the following syntax:  
`client.rpc.NotifyAsync("SendDataAsync", Plugin Identifier, Data Identifier, Any Object);`  
While the Plugin identifier is used to know to which WebSocket connection the data need to be sent, the purpose of the Data Identifier is to easily identifiable once it's received by the overlay's Websocket Client, it can be the position of the cursor for example.
 (E.G: `Data Identifier = "Position"`)  
Any type of data can be sent provided that it can be serialized by newtonsoft.
- Second, your overlay need to send an initial request with the Plugin Identifier (see example).  
If the identifier is correct then you should be receiving data.