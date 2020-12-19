using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Test
{
    public class TestClient
    {

        private readonly Socket _socket;

        private readonly byte[] _buffer = new byte[1024];

        private string Message { get; set; }

        public TestClient()
        {
            var ipAddress = IPAddress.Loopback;
            var server = new IPEndPoint(ipAddress, 10000);
            _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _socket.Connect(server);
        }

        public string SendMessage(string message)
        {
            var data = Encoding.ASCII.GetBytes(message);
            var result = _socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendData, _socket);
            while (!result!.IsCompleted)
                Thread.Sleep(25);
            return message;
        }

        public void DiscardEcho()
        {
            ReceiveMessage();
        }

        public string ReceiveMessage()
        {
            var result = _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveData, _socket);
            while (!result!.IsCompleted)
                Thread.Sleep(25);
            var returnValue = Message?.Trim();
            Message = "";
            return returnValue;
        }

        private void SendData(IAsyncResult result)
        {
            try
            {
                _socket.EndSend(result);
            }
            catch
            {
                // ignored
            }
        }

        private void ReceiveData(IAsyncResult result)
        {
            var socket = (Socket) result.AsyncState;
            var bytesReceived = socket!.EndReceive(result);
            var byteMessage = _buffer.Take(bytesReceived).Where(d => d < 0xF0).ToArray();
            Message = Encoding.ASCII.GetString(byteMessage, 0, byteMessage.Length);
        }

        public void Close()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

    }
}