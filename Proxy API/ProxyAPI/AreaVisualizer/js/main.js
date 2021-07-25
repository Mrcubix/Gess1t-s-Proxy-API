var tabletAreaContainer;
var userDefinedArea;
var fullArea;
var area;
var penPositionContainer;
var penCursorPositionCircle;
var areaUpdateInterval;
var userDefinedAreaContainer;

document.addEventListener('DOMContentLoaded', async function () {   
    tabletAreaContainer = document.getElementsByClassName("FullAreaContainer")[0];
    userDefinedAreaContainer = document.getElementsByClassName("UserDefinedAreaContainer")[0];
    penPositionContainer = document.getElementsByClassName("PenPositionContainer")[0];
    fullArea = document.getElementById("FullArea");
    userDefinedArea = document.getElementById("UserDefinedArea");
    penCursorPositionCircle = document.getElementById("PenPosition");

    let request = await fetch("/SocketPort");
    let text = await request.text();
    let port = parseInt(text);

    let webSocketURL = "ws://localhost:"+port;

    var socket = new WebSocket(webSocketURL);

    socket.onopen = function (event) {
        console.log("Connected, send request now...");
        socket.send('{"id": "AreaVisualizer"}');
        console.log("Initial Query sent");
    };
    socket.onmessage = function (message) {
        json = JSON.parse(message.data);
        if (json.Area != undefined) {
            area = json["Area"];
            SetArea(area);
        } else {
            SetPos(json["Position"]);
        }
    }
})
function SetPos(pos) {
    if (area != undefined) {
        penCursorPositionCircle.setAttribute("cx", String(pos.X / area.lpmm)+"mm");
        penCursorPositionCircle.setAttribute("cy", String(pos.Y / area.lpmm)+"mm");
    }
}
function SetArea(area) {
    if (area != undefined) {
        tabletAreaContainer.setAttribute("width", String(area.fullArea.size.X)+"mm");
        tabletAreaContainer.setAttribute("height", String(area.fullArea.size.Y)+"mm");

        userDefinedAreaContainer.setAttribute("height", String(area.fullArea.size.Y)+"mm");
        userDefinedAreaContainer.setAttribute("height", String(area.fullArea.size.Y)+"mm");

        penPositionContainer.setAttribute("width", String(area.fullArea.size.X)+"mm");
        penPositionContainer.setAttribute("height", String(area.fullArea.size.Y)+"mm");

        fullArea.setAttribute("width", String(area.fullArea.size.X)+"mm");
        fullArea.setAttribute("height", String(area.fullArea.size.Y)+"mm");
        fullArea.setAttribute("x", "0mm");
        fullArea.setAttribute("y", "0mm");
        
        userDefinedArea.setAttribute("width", String(area.size.X)+"mm");
        userDefinedArea.setAttribute("height", String(area.size.Y)+"mm");
        userDefinedArea.setAttribute("x", String(area.position.X - (area.size.X / 2))+"mm");
        userDefinedArea.setAttribute("y", String(area.position.Y - (area.size.Y / 2))+"mm");
    }
}