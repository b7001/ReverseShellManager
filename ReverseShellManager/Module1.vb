Imports System.Net.Sockets
Imports System.Text
Module Module1
    Dim lPort As Long = 8888
    Dim selClient As Integer = -10
    Dim clientList As New List(Of handleClient)
    Dim serverSocket As TcpListener
    Dim shellMode As Boolean
    Friend WithEvents tmrDisconnectCheck As New Timers.Timer


    Sub Main()
        Dim sArgs() As String = System.Environment.GetCommandLineArgs()

        If (sArgs.Length > 1) Then
            If (IsNumeric(sArgs(1))) Then lPort = sArgs(1)
        End If

        serverSocket = New TcpListener(System.Net.IPAddress.Any, lPort)

        Dim thCMDListener As Threading.Thread = New Threading.Thread(AddressOf cmdListener)
        thCMDListener.Start()

        Dim thListener As Threading.Thread = New Threading.Thread(AddressOf sListener)
        thListener.Start()

        tmrDisconnectCheck.Interval = 5000
        tmrDisconnectCheck.Enabled = True
    End Sub


    Sub cmdListener()


        Dim cmd As String = ""

        Do While True
            writePrompt()
            cmd = Console.ReadLine()
            parseCmd(cmd)
        Loop
    End Sub

    Sub writePrompt()
        If (shellMode = True) Then
            Console.ForegroundColor = ConsoleColor.Black
            Console.BackgroundColor = ConsoleColor.Green
            Console.Write("sh>")
            Console.ResetColor()

        Else
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write(">>>")
            Console.ResetColor()
        End If
    End Sub

    Sub doSend(ByVal cmd As String)
        Dim s As handleClient = clientList(selClient)
        If (cmd <> "") Then
            Dim sendBytes As Byte()
            Dim networkStream As NetworkStream = s.clientSocket.GetStream()
            sendBytes = Encoding.ASCII.GetBytes(cmd)
            networkStream.Write(sendBytes, 0, sendBytes.Length)
            networkStream.Flush()
        End If
    End Sub

    Sub checkDisconnected()

        For i = 0 To (clientList.Count - 1)

            Dim blockingState As Boolean = clientList(i).clientSocket.Client.Blocking
            Try
                Dim tmp(0) As Byte
                clientList(i).clientSocket.Client.Blocking = False
                clientList(i).clientSocket.Client.Send(tmp, 0, 0)
                clientList(i).clientSocket.Client.Blocking = blockingState
            Catch e As SocketException
                If e.NativeErrorCode <> 10035 Then
                    'Console.WriteLine("Disconnected: error code {0}!", e.NativeErrorCode)
                    dbg("DISCONNECTED - " & clientList(i).remoteIP & ":" & clientList(i).remotePORT, "+")
                    clientList.Remove(clientList(i))
                    If (selClient = i) Then
                        selClient = -10
                    End If
                Else
                    clientList(i).clientSocket.Client.Blocking = blockingState
                End If
            Catch ex As Exception
                dbg("DISCONNECTED - " & clientList(i).remoteIP & ":" & clientList(i).remotePORT, "+")
                clientList.Remove(clientList(i))
                If (selClient = i) Then
                    selClient = -10
                End If
                Return
            End Try

        Next
    End Sub

    Sub parseCmd(ByVal cmd As String)
        Dim spCMD() As String

        If (shellMode = True) Then
            If (cmd = "exit") Then
                shellMode = False
                dbg("SHELL returned to background")
            Else
                doSend(cmd)
            End If
        End If

        If (shellMode = False) Then
            spCMD = Split(cmd, " ")

            Select Case spCMD(0)
                Case "list"
                    displayClientList()

                Case "help", "h", "?"
                    dbg("#### HELP ####", "?")
                    dbg("list - List all connected clients", "?")
                    dbg("select <id> - Select client for further actions", "?")
                    dbg("shell - Interactive shell to selected client", "?")
                    dbg("client - Currently selected client info", "?")
                    dbg("kill <id> - Close connection of client", "?")
                    dbg("help - Currently selected client info", "?")
                    dbg("about - Some shameless self promotion", "?")
                    dbg("#### END HELP ####", "?")


                Case "about"
                    dbg("Designed by b7001 (https://github.com/b7001)", "i")


                Case "client"
                    dbg("Current Client - [" & selClient & "] - " & clientList(selClient).remoteIP & ":" & clientList(selClient).remotePORT)


                Case "select"
                    If (spCMD.Length > 0) Then
                        If ((clientList.Count - 1) >= spCMD(1)) Then
                            selClient = spCMD(1)
                            dbg("Selected [" & selClient & "] - " & clientList(selClient).remoteIP & ":" & clientList(selClient).remotePORT)
                        Else
                            dbg("Error in select")
                        End If
                    End If


                Case "kill"
                    If (spCMD.Length > 0) Then
                        If ((clientList.Count - 1) >= spCMD(1)) Then
                            'selClient = spCMD(1)
                            dbg("Killing [" & spCMD(1) & "] - " & clientList(spCMD(1)).remoteIP & ":" & clientList(spCMD(1)).remotePORT)
                            clientList(spCMD(1)).clientSocket.Close()
                            'clientList.Remove(clientList(spCMD(1)))
                            checkDisconnected()
                        Else
                            dbg("Error in kill")
                        End If
                    End If

                Case "shell"
                    If (selClient <> -10) Then
                        dbg("Connected to SHELL of: [" & selClient & "] - " & clientList(selClient).remoteIP & ":" & clientList(selClient).remotePORT)
                        shellMode = True

                    End If
            End Select
        End If

    End Sub

    Sub displayClientList()
        dbg("Clients Connected: " & clientList.Count, "?")
        For i = 0 To (clientList.Count - 1)
            dbg("[" & i & "] - " & clientList(i).remoteIP & ":" & clientList(i).remotePORT)
        Next
    End Sub

    Sub sListener()

        Dim clientSocket As TcpClient
        Dim counter As Integer
        serverSocket.Start()
        dbg("Server Started on 0.0.0.0:" & lPort)
        counter = 0
        While (True)
            counter += 1
            clientSocket = serverSocket.AcceptTcpClient()
            Dim client As New handleClient
            client.startClient(clientSocket, Convert.ToString(counter))
            clientList.Add(client)
            client.clientID = clientList.IndexOf(client)
            client.remoteIP = CType(client.clientSocket.Client.RemoteEndPoint, System.Net.IPEndPoint).Address.ToString()
            client.remotePORT = CType(client.clientSocket.Client.RemoteEndPoint, System.Net.IPEndPoint).Port.ToString()



            dbg("CONNECTED - " & client.remoteIP & ":" & client.remotePORT, "+")
        End While

        clientSocket.Close()
        serverSocket.Stop()
        dbg("Closing. Bye", "-")
        Console.ReadLine()
    End Sub

    Sub dbg(ByVal msg As String, Optional ByVal mode As String = "")
        Dim tag As String = ""
        Dim cl As Integer = 0

        msg.Trim()

        If (mode = "") Then
            tag = "[+]"
        End If

        Select Case mode
            Case "i"
                tag = "[i]"
                cl = ConsoleColor.Yellow
            Case "?"
                tag = "[?]"
                cl = ConsoleColor.Gray
            Case "-"
                tag = "[-]"
                cl = ConsoleColor.Red
            Case "+"
                tag = "[+]"
                cl = ConsoleColor.Magenta
            Case Else
                tag = "[i]"
                cl = ConsoleColor.Gray
        End Select

        Console.ForegroundColor = cl
        Console.WriteLine(tag & " " + msg)
        Console.ResetColor()

    End Sub


    Sub doOutputCheck(ByVal s As String, ByVal c As Integer)
        If (c = selClient) Then
            Console.WriteLine(s)
            Console.WriteLine("")
            writePrompt()
        End If

    End Sub


    Public Class handleClient
        Public clientSocket As TcpClient

        Property remoteIP As String = ""
        Property remotePORT As String = ""
        Property clientID As Integer = 0

        Dim clNo As String
        Public Sub startClient(ByVal inClientSocket As TcpClient, ByVal clineNo As String)
            Me.clientSocket = inClientSocket
            Me.clNo = clineNo
            Dim ctThread As Threading.Thread = New Threading.Thread(AddressOf doRecv)
            ctThread.Start()
        End Sub





        Private Sub doRecv()
            Dim dataFromClient As String
            Dim bytesRead As Integer = 0
            Dim bytesFrom(clientSocket.ReceiveBufferSize) As Byte



            While (True)
                Try
                    If (clientSocket.Connected) Then
                        System.Threading.Thread.Sleep(1)

                        Dim networkStream As NetworkStream = clientSocket.GetStream()
                        If (networkStream.CanRead And networkStream.DataAvailable) Then

                            bytesRead = networkStream.Read(bytesFrom, 0, CInt(clientSocket.ReceiveBufferSize))
                            dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom, 0, bytesRead)
                            While (networkStream.CanRead And networkStream.DataAvailable)
                                bytesRead = networkStream.Read(bytesFrom, 0, bytesFrom.Length)
                                dataFromClient = dataFromClient & System.Text.Encoding.ASCII.GetString(bytesFrom, 0, bytesRead)
                            End While

                            doOutputCheck(dataFromClient, Me.clientID)


                        End If
                    End If

                Catch e As SocketException
                    If e.NativeErrorCode <> 10035 Then
                        'Console.WriteLine("Disconnected: error code {0}!", e.NativeErrorCode)
                        dbg("DISCONNECTED - " & clientList(Me.clientID).remoteIP & ":" & clientList(Me.clientID).remotePORT, "+")
                        clientList.Remove(clientList(Me.clientID))
                        If (selClient = Me.clientID) Then
                            selClient = -10
                        End If
                        Return
                    Else
                        clientList(Me.clientID).clientSocket.Client.Blocking = True
                    End If
                Catch ex As Exception
                    'dbg("DISCONNECTED - " & clientList(Me.clientID).remoteIP & ":" & clientList(Me.clientID).remotePORT, "+")
                    'clientList.Remove(clientList(Me.clientID))
                    'If (selClient = Me.clientID) Then
                    'selClient = -10
                    'End If
                    Return
                End Try

            End While

        End Sub
    End Class


    Private Sub tmrDisconnectCheck_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles tmrDisconnectCheck.Elapsed
        checkDisconnected()
        tmrDisconnectCheck.Enabled = True
    End Sub
End Module