# WebConsole
WebConsole is a server that allows clients to communicate with console based programs running on the same computer as the server. Reproduces a feature available on the control panel of many hosting services. 

This project was made using https://github.com/jrghndl/Web-Suite
If you need a better understanding of the code used in this project, read WebSuite's documentation in the wiki.

## Trying WebConsole

 1. Run WebConsole.exe
 2. Connect through a browser to [http://127.0.0.1:50000/](http://127.0.0.1:50000/). This is the client.
 3. Login. A sample user already exists. (Username: testuser) (Password: password)
 4. WebConsole connects directly to command prompt, so you can execute any command from command prompt directly on the client.  Try "ping 127.0.0.1" or "ipconfig /all" as  an example.


## Commands
The following is a list of commands for the WebConsole server, not the client.

    new account [username] [password] [role]
Creates a new account with the above credentials. Role is required, but its unimplemented.

    new redirect [originalURL] [redirectURL]
If a user navigates to the webpage at originalURL, they are redirected to redirectURL.

    maintenance [true/false]
Enables maintenance mode. Every page is redirected to the maintenance URL. Users can be given a message saying that the website is under maintenance.

    alert [message]
    
Broadcasts a message to every client connected to the server. This includes both clients that are logged in and those that are not.

    qinfo
Returns the size of the message queue.
