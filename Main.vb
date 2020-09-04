Module Main
    Dim Database As WebSuite.XMLDatabase
    Dim WebServer As WebSuite.HTTPServer
    Dim WebsocketServer As WebSuite.WebsocketServer
    Dim Proc As Process
    Sub Main()
        Initialize()

        'Starts command prompt
        Proc = New Process
        With Proc.StartInfo
            .FileName = "cmd.exe"
            .Arguments = ""
            .RedirectStandardOutput = True
            .RedirectStandardError = True
            .RedirectStandardInput = True
            .UseShellExecute = False
            '.WorkingDirectory = "C:\Users\jrghndl\Desktop"
        End With

        'Redirects output
        AddHandler Proc.OutputDataReceived, AddressOf ProcessMessage
        AddHandler Proc.ErrorDataReceived, AddressOf ProcessError
        Proc.Start()
        Proc.BeginErrorReadLine()
        Proc.BeginOutputReadLine()
        Proc.StandardInput.WriteAsync("")


        'Creates message queue and timer for sending messages in message queue.
        Dim Timer As New Timers.Timer(50)
        AddHandler Timer.Elapsed, AddressOf SendQueuedMessage
        Timer.Start()

        'Handles user's input on the WebConsole server.
        Do
            Dim Command() As String = Console.ReadLine.Split(" ")
            Try
                Select Case Command(0).ToLower
                    Case "exit"
                        Exit Do
                    Case "new"
                        If Command(1).ToLower = "account" Then
                            AddAccount(Command(2), Command(3), Command(4))
                            Console.WriteLine("Account successfully added")
                        ElseIf Command(1).ToLower = "redirect" Then
                            If WebServer.RedirectExists(Command(2)) Then
                                WebServer.DeleteRedirect(Command(2))
                            End If
                            WebServer.AddRedirect(Command(2), Command(3))
                            Console.WriteLine("Redirect successfully created/modified.")
                        Else
                            Console.WriteLine("Invalid. Can only create new Account or Redirect.")
                        End If
                    Case "maintenance"
                        If Command(1).ToLower = "true" Then
                            WebServer.UnderMaintenance = True
                            Console.WriteLine("Maintenance mode has been activated.")
                        ElseIf Command(1).ToLower = "false" Then
                            WebServer.UnderMaintenance = False
                            Console.WriteLine("Maintenance mode has been deactivated.")
                        Else
                            Console.WriteLine("Invalid Option")
                        End If
                    Case "alert"
                        Dim Message As New WebSuite.JSONBuilder
                        Message.Add("Type", "Message")
                        Message.Add("Message", Command(1))
                        WebsocketServer.Broadcast(Message.Data)
                    Case "qinfo"
                        Console.WriteLine("Message Queue size: " + MessageQueue.Count.ToString)
                    Case Else
                        Console.WriteLine("Unknown Command")
                End Select
            Catch ex As Exception
                Console.WriteLine("An error occurred.")
            End Try
        Loop

        Proc.Kill()
        WebServer.Shutdown()
        WebsocketServer.Shutdown()
    End Sub

    'Initializes the http server, websocket server, and database. Creates new database if one doesn't exist already.
    Sub Initialize()
        CheckFolder("Root")
        CheckFolder("Data")
        CheckFolder("Certificates")
        If CheckFile("Data/Database.xml") Then
            Database = New WebSuite.XMLDatabase
            Database.LoadFromFile("Data/Database.xml")
        Else
            Database = New WebSuite.XMLDatabase("Database")
            Database.AddTable(New WebSuite.XMLTable("Accounts"))
            Database.AddTable(New WebSuite.XMLTable("Redirects"))
            Dim IDColumn As New WebSuite.XMLColumn("ID")
            IDColumn.Unique = True
            IDColumn.AutoIncrement = True
            Database.Table("Accounts").AddColumn(IDColumn)
            Database.Table("Accounts").AddColumn("Username")
            Database.Table("Accounts").AddColumn("Password")
            Database.Table("Accounts").AddColumn("Role")
            Database.Table("Accounts").AddColumn("DateCreated")
            Database.Table("Accounts").AddColumn("LastLogin")
            Database.Table("Accounts").SetPrimaryKey("ID")
            Database.Table("Redirects").AddColumn(IDColumn)
            Database.Table("Redirects").AddColumn("From")
            Database.Table("Redirects").AddColumn("To")
            Database.Table("Redirects").AddColumn("DateCreated")
            Database.Table("Redirects").SetPrimaryKey("ID")
            Database.SaveDatabase("Data/Database.xml")
        End If

        WebServer = New WebSuite.HTTPServer()
        WebServer.Port = 50000
        WebServer.Verbose = True
        WebServer.UseSSL = False
        'WebServer.CertificatePath = "Certificates/Cert.pfx"
        'WebServer.CertificatePassword = "password"
        WebServer.RootPath = "Root"
        WebServer.NotFoundPath = "/404.html"
        WebServer.MaintenancePath = "/maintenance.html"
        WebServer.Start()


        WebsocketServer = New WebSuite.WebsocketServer

        WebsocketServer.Port = 50001
        WebsocketServer.Functions.OnOpenFunc = AddressOf OnOpen
        WebsocketServer.Functions.OnMessageFunc = AddressOf OnMessage
        WebsocketServer.Functions.OnCloseFunc = AddressOf OnClose
        WebsocketServer.Functions.OnErrorFunc = AddressOf OnError
        WebsocketServer.Start()

    End Sub

    'Creates folder if given path to folder does not exist.
    Sub CheckFolder(ByVal FolderPath As String)
        If Not My.Computer.FileSystem.DirectoryExists(FolderPath) Then
            My.Computer.FileSystem.CreateDirectory(FolderPath)
        End If
    End Sub

    'Checks whether a file exists.
    Function CheckFile(ByVal FilePath As String) As Boolean
        If Not My.Computer.FileSystem.FileExists(FilePath) Then
            Return False
        End If
        Return True
    End Function

    'Adds an account to the database. Only logged in users can send messages to the server through the web client.
    Public Sub AddAccount(ByVal Username As String, ByVal Password As String, ByVal Role As String)
        Dim Record As WebSuite.XMLRecord = Database.Table("Accounts").NewRecord
        Record.SetValue("Username", Username)
        Record.SetValue("Password", Password) 'Encrypt password in the future
        Record.SetValue("Role", Role)
        Record.SetValue("DateCreated", Date.UtcNow)
        Record.Insert()
        Database.SaveDatabase("Data/Database.xml")
        Console.WriteLine("Account has been created successfully.")
    End Sub

    'OnOpen is called when a new connection is established between a client and the server.
    Sub OnOpen(ByVal id As String)
        LoggedIn.Add(id, False)
        Console.WriteLine("New connection established to client (" + id + ")")
        Dim Message As New WebSuite.JSONBuilder
        Message.Add("Type", "Message")
        Message.Add("Message", "Hello from the server!")
        WebsocketServer.SendMessage(id, Message.Data)
    End Sub

    Dim LoggedIn As New Dictionary(Of String, Boolean)

    'OnMessage is called when a message is received from a client.
    Sub OnMessage(ByVal id As String, ByVal data As String)


        Dim Message As New WebSuite.JSONObject(data)
        Select Case Message.GetElement("Type").ToString
            Case "Login"
                If LoggedIn(id) Then
                    Dim Response As New WebSuite.JSONBuilder
                    Response.Add("Type", "Login")
                    Response.Add("Success", "False")
                    Response.Add("Reason", "Already Logged In")
                    WebsocketServer.SendMessage(id, Response.Data)
                    Exit Sub
                Else
                    Dim Record() As WebSuite.XMLRecord = Database.Table("Accounts").GetRecords("Username = '" + Message.GetElement("Username").ToString + "'")
                    If Record.Length > 0 Then
                        If Record(0).GetValue("Password").Equals(Message.GetElement("Password").ToString) Then
                            Dim Response As New WebSuite.JSONBuilder
                            Response.Add("Type", "Login")
                            Response.Add("Success", "True")
                            WebsocketServer.SendMessage(id, Response.Data)
                            LoggedIn(id) = True
                        Else
                            Dim Response As New WebSuite.JSONBuilder
                            Response.Add("Type", "Login")
                            Response.Add("Success", "False")
                            Response.Add("Reason", "Incorrect Password.")
                            WebsocketServer.SendMessage(id, Response.Data)
                        End If
                    Else
                        Dim Response As New WebSuite.JSONBuilder
                        Response.Add("Type", "Login")
                        Response.Add("Success", "False")
                        Response.Add("Reason", "Username Doesn't Exist.")
                        WebsocketServer.SendMessage(id, Response.Data)
                    End If
                End If
            Case "Logout"
                If LoggedIn(id) Then
                    LoggedIn(id) = False
                End If
            Case "Command"
                If LoggedIn(id) Then
                    Proc.StandardInput.WriteLine(Message.GetElement("Command").ToString)
                End If
        End Select

        Console.WriteLine(data)
    End Sub

    'OnClose is called when a connection between a client and the server is closed.
    Sub OnClose(ByVal id As String, ByVal code As String, ByVal reason As String)
        WebsocketServer.CloseSession(id)
        'Console.WriteLine("Code: {0} |Reason: {1}", code, reason)
    End Sub

    'OnError is called when there's a websocket error.
    Sub OnError(ByVal id As String, ByVal message As String, ByVal exception As Exception)
        WebsocketServer.CloseSession(id)
        Console.WriteLine("Error Message: {0}" + Environment.NewLine + "{1}", message, exception.ToString)
    End Sub

    'Queues messages that are redirected from the command prompt based process.
    Public Sub ProcessMessage(Sender As Object, e As DataReceivedEventArgs)
        For Each KeyPair As KeyValuePair(Of String, Boolean) In LoggedIn
            If KeyPair.Value Then
                Dim Message As New WebSuite.JSONBuilder
                Message.Add("Type", "Message")
                Message.Add("Message", e.Data.ToString)
                Try
                    MessageQueue.Enqueue(New MessageStructure(KeyPair.Key, Message.Data))
                    'WebsocketServer.SendMessage(KeyPair.Key, Message.Data)
                Catch ex As Exception
                    Console.WriteLine(ex.Message)
                    Console.WriteLine(ex.InnerException)
                    Console.WriteLine(ex.StackTrace)
                    Console.WriteLine(ex.HResult)
                End Try
            End If
        Next
    End Sub

    'Queues errors that are redirected from the command prompt based process.
    Public Sub ProcessError(Sender As Object, e As DataReceivedEventArgs)
        For Each KeyPair As KeyValuePair(Of String, Boolean) In LoggedIn
            If KeyPair.Value Then
                Dim Message As New WebSuite.JSONBuilder
                Message.Add("Type", "Message")
                Message.Add("Message", e.Data.ToString)
                Try
                    MessageQueue.Enqueue(New MessageStructure(KeyPair.Key, Message.Data))
                    'WebsocketServer.SendMessage(KeyPair.Key, Message.Data)
                Catch ex As Exception
                    Console.WriteLine(ex.Message)
                    Console.WriteLine(ex.InnerException)
                    Console.WriteLine(ex.StackTrace)
                    Console.WriteLine(ex.HResult)
                End Try
            End If
        Next
    End Sub

    'Structure of a basic message
    Public Structure MessageStructure
        Public ID As String
        Public Message As String
        Public Sub New(ByVal _ID As String, ByVal _Message As String)
            ID = _ID
            Message = _Message
        End Sub
    End Structure

    'Sends messages to connected clients based on the order of the queue.
    Dim MessageQueue As New Queue(Of MessageStructure)
    Public Sub SendQueuedMessage()
        Dim Response As MessageStructure = MessageQueue.Dequeue
        WebsocketServer.SendMessage(Response.ID, Response.Message)
    End Sub

End Module
