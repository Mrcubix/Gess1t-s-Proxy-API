var json;

document.addEventListener('DOMContentLoaded', async function () {  
    let request = await fetch("/SocketPort");
    let text = await request.text();
    let port = parseInt(text);

    let webSocketURL = "ws://localhost:"+port;

    var socket = new WebSocket(webSocketURL);

    socket.onopen = function (event) {
        socket.send('{"pipe": "AreaRandomizer"}');
        console.log("Initial Query sent");
    };
    socket.onmessage = function (message) {
        var json = JSON.parse(message.data);
    }
})