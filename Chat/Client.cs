using System;
using System.Net;

namespace Chat
{
    public class Client
    {

        public readonly byte[] Buffer = new byte[1024];
        
        private readonly uint _id;

        private readonly IPEndPoint _remoteAddr;

        private readonly DateTime _connectedAt;

        private string _receivedData;

        private string _username;

        public Client(uint clientId,
                      IPEndPoint remoteAddress)
        {
            _id = clientId;
            _remoteAddr = remoteAddress;
            _connectedAt = DateTime.Now;
            _receivedData = string.Empty;
        }

        public uint GetClientId()
        {
            return _id;
        }

        public string GetUsername()
        {
            return _username;
        }

        public void SetUsername(string username)
        {
            _username = username;
        }

        public string GetReceivedData()
        {
            return _receivedData;
        }

        public void AppendReceivedData(string dataToAppend)
        {
            _receivedData += dataToAppend;
        }

        public void RemoveLastCharacterReceived()
        {
            _receivedData = _receivedData.Substring(0, _receivedData.Length - 1);
        }

        public void ResetReceivedData()
        {
            _receivedData = string.Empty;
        }

        public override string ToString()
        {
            var ip = $"{_remoteAddr.Address.ToString()}:{_remoteAddr.Port}";
            var res = $"Client #{_id} (From: {ip}, Connection time: {_connectedAt})";
            return res;
        }

    }
}