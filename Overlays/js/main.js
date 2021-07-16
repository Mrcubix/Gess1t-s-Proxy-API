/*
    TODO:
        - Make a proper UI
*/
var json;

document.addEventListener('DOMContentLoaded', async function () {  
    var list = document.querySelector("ul"); 
    let request = await fetch("/list");
    json = await request.json();
    json.data.forEach(function(item) {
        url = "/"+item+"/index.html";
        var element = document.createElement("li");
        var link = document.createElement("a");
        link.href = url;
        link.textContent = item;
        element.appendChild(link);
        list.appendChild(element);
    })
});