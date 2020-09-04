var wsUri = "ws://127.0.0.1:50001/websocket";
var websocket = new WebSocket(wsUri);

websocket.onopen = function (e) {
    addLine("CONNECTED");
};

websocket.onclose = function (e) {
    addLine("DISCONNECTED");
};

websocket.onmessage = function (e) {
    console.log(e.data);
    var jsonObject = JSON.parse(e.data);

    switch(jsonObject.Type) {
        case "Login":
            if(jsonObject.Success === "True"){
                addLine("Successful Login");
                document.getElementById("user").disabled = true;
                document.getElementById("pass").disabled = true;
                document.getElementById("login").disabled = true;
            } else {
                addLine(jsonObject.Reason);
            }
            break;
        case "Message":
            addLine(jsonObject.Message);
            break;
        default:
            addLine("Unknown message from server");
    }
};

websocket.onerror = function (e) {
    addLine("ERROR: " + e.data);
};




function login(){
    var message = { Type: "Login", Username: document.getElementById("user").value, Password: document.getElementById("pass").value };
    websocket.send(JSON.stringify(message));
}

function sendMessage() {
    let event = window.event || event.which;
    if (event.key === "Enter") {
        event.preventDefault();
        var message = { Type: "Command", Command: document.getElementById("input").value };
        websocket.send(JSON.stringify(message));
        addLine(document.getElementById("input").value);
        document.getElementById("input").value = "";
        document.getElementById("input").focus()
    }
}

function addLine(line) {
    var today = new Date();
    var time = (today.getHours()<10?'0':'') + today.getHours() + ":" + (today.getMinutes()<10?'0':'') + today.getMinutes() + ":" + (today.getSeconds()<10?'0':'') + today.getSeconds();
    let textNode = document.createTextNode("[" + time + "] " + line);
    let br = document.createElement("br");
    document.getElementById("consoleText").appendChild(textNode);
    document.getElementById("consoleText").appendChild(br);
    document.getElementById("consoleText").scrollTop = document.getElementById("consoleText").scrollHeight
}
