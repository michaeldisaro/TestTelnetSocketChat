using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Chat
{
    public class Server
    {

        private const int Port = 10000;

        public const string EndLine = "\r\n";

        private readonly Socket _serverSocket;

        private readonly IPAddress _ipAddress;

        private readonly Dictionary<Socket, Client> _clients;

        private readonly bool _logMessages;

        public Server(IPAddress ipAddress,
                      bool logMessages = true)
        {
            _ipAddress = ipAddress;
            _logMessages = logMessages;
            _clients = new Dictionary<Socket, Client>();
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Start()
        {
            _serverSocket.Bind(new IPEndPoint(_ipAddress, Port));
            _serverSocket.Listen(0);
            _serverSocket.BeginAccept(HandleIncomingConnection, _serverSocket);
        }

        public void Stop()
        {
            _serverSocket.Close();
        }

        public void SendMessageToClient(Client c,
                                        string message)
        {
            var clientSocket = GetSocketByClient(c);
            SendMessageToSocket(clientSocket, message);
        }

        private void SendMessageToSocket(Socket s,
                                         string message)
        {
            var data = Encoding.ASCII.GetBytes(message);
            SendBytesToSocket(s, data);
        }

        private void SendBytesToSocket(Socket s,
                                       byte[] data)
        {
            s.BeginSend(data, 0, data.Length, SocketFlags.None, SendData, s);
        }

        public void SendMessageToOthersThan(string message,
                                            uint excludeClientId)
        {
            foreach (var s in _clients.Keys)
            {
                try
                {
                    var c = _clients[s];
                    if (c.GetClientId() == excludeClientId) continue;
                    SendMessageToSocket(s, message + EndLine);
                    c.ResetReceivedData();
                }
                catch
                {
                    _clients.Remove(s);
                }
            }
        }

        private Client GetClientBySocket(Socket clientSocket)
        {
            Client c;
            if (!_clients.TryGetValue(clientSocket, out c))
                c = null;
            return c;
        }

        private Socket GetSocketByClient(Client client)
        {
            Socket s;
            s = _clients.FirstOrDefault(x => x.Value.GetClientId() == client.GetClientId()).Key;
            return s;
        }

        private void CloseSocket(Socket clientSocket)
        {
            clientSocket.Close();
            _clients.Remove(clientSocket);
        }

        private void HandleIncomingConnection(IAsyncResult result)
        {
            try
            {
                var oldSocket = (Socket) result.AsyncState;
                if (oldSocket == null) return;

                var newSocket = oldSocket.EndAccept(result);
                var clientId = (uint) _clients.Count + 1;
                var client = new Client(clientId, (IPEndPoint) newSocket.RemoteEndPoint);
                _clients.Add(newSocket, client);

                // SEND TELNET STANDARD CONTROL CHARS
                SendBytesToSocket(newSocket, new byte[]
                {
                    0xff, 0xfd, 0x01, // Do Echo
                    0xff, 0xfd, 0x21, // Do Remote Flow Control
                    0xff, 0xfb, 0x01, // Will Echo
                    0xff, 0xfb, 0x03 // Will Supress Go Ahead
                });

                client.ResetReceivedData();
                SendMessageToClient(client, "What's your name?" + EndLine);
                _serverSocket.BeginAccept(HandleIncomingConnection, _serverSocket);
            }
            catch
            {
                // ignored
            }
        }

        private void SendData(IAsyncResult result)
        {
            try
            {
                var clientSocket = (Socket) result.AsyncState;
                if (clientSocket == null) return;
                clientSocket.EndSend(result);
                var client = GetClientBySocket(clientSocket);
                clientSocket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None, ReceiveData,
                                          clientSocket);
            }
            catch
            {
                // ignored
            }
        }

        private void ReceiveData(IAsyncResult result)
        {
            try
            {
                var clientSocket = (Socket) result.AsyncState;
                if (clientSocket == null) return;
                var client = GetClientBySocket(clientSocket);

                var bytesReceived = clientSocket.EndReceive(result);
                if (bytesReceived == 0)
                {
                    CloseSocket(clientSocket);
                    _serverSocket.BeginAccept(HandleIncomingConnection, _serverSocket);
                }
                else if (client.Buffer[0] < 0xF0)
                {
                    var receivedData = client.GetReceivedData();

                    // 0x2E = '.', 0x0D = carriage return, 0x0A = new line
                    if ((client.Buffer[0] == 0x2E && client.Buffer[1] == 0x0D && receivedData.Length == 0) ||
                        (client.Buffer[0] == 0x0D && client.Buffer[1] == 0x0A))
                    {
                        var message = client.GetReceivedData();
                        if (client.GetUsername() == null)
                        {
                            if (message.Length > 15) message = message.Substring(0, 15);
                            client.SetUsername(message);
                            SendMessageToClient(client, EndLine + $"You can start chatting {message}!" + EndLine);
                        }
                        else
                        {
                            MessageReceived(client, message);
                        }

                        client.ResetReceivedData();
                    }
                    else
                    {
                        switch (client.Buffer[0])
                        {
                            // 0x08 => backspace character
                            case 0x08 when receivedData.Length > 0:
                                client.RemoveLastCharacterReceived();
                                SendBytesToSocket(clientSocket, new byte[] {0x08, 0x20, 0x08});
                                break;

                            // 0x7F => delete character
                            case 0x08:
                            case 0x7F:
                                clientSocket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None,
                                                          ReceiveData,
                                                          clientSocket);
                                break;
                            default:
                            {
                                client.AppendReceivedData(Encoding.ASCII.GetString(client.Buffer, 0, bytesReceived));

                                // Echo back the received character
                                SendBytesToSocket(clientSocket, new[] {client.Buffer[0]});

                                clientSocket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None,
                                                          ReceiveData,
                                                          clientSocket);
                                break;
                            }
                        }
                    }
                }
                else
                    clientSocket.BeginReceive(client.Buffer, 0, client.Buffer.Length, SocketFlags.None, ReceiveData,
                                              clientSocket);
            }
            catch
            {
                // ignored
            }
        }

        private void MessageReceived(Client c,
                                     string message)
        {
            if (_logMessages) Console.WriteLine("MESSAGE: " + message);
            SendMessageToClient(c, Server.EndLine);
            SendMessageToOthersThan($"{c.GetUsername()}: {message}", c.GetClientId()); // comment this...
            // SendMessageToOthersThan($"{c.GetUsername()}:{message}", c.GetClientId()); // ...and uncomment this to try an error in tests
        }

    }
}