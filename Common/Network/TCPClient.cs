using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using Common.Cipher;
using MessagePack;
using System.Net;
using Common.Log;

namespace Common.Network
{
    public class TCPClient : IDisposable
    {
        public string host;
        public int port;
        public Dictionary<string, object> Storage = new();

        Aes256Gcm _aes;
        Socket _socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public TCPClient(string host, int port, string aesPwd)
        {
            this.host = host;
            this.port = port;
            _aes = new Aes256Gcm(aesPwd);
        }
        public TCPClient(IPAddress ip, int port, string aesPwd)
        {
            host = ip.ToString();
            this.port = port;
            _aes = new Aes256Gcm(aesPwd);
        }

        public TCPClient(Socket clientSocket, Aes256Gcm aesInstance)
        {
            IPEndPoint? ipEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
            if (ipEndPoint == null)
                throw new Exception("Invalid IPEndPoint");

            host = ipEndPoint.Address.ToString();
            port = ipEndPoint.Port;

            _socket = clientSocket;
            _aes = aesInstance;
        }

        // BeginConnect is called by the client to initiate the connection to the server asynchronously.
        // success delegate is called when the connection is established.
        // error delegate is called if the connection fails.
        public void BeginConnect(Action success, Action<Exception> exception)
        {
            _socket.BeginConnect(host, port, (ar) =>
            {
                try
                {
                    _socket.EndConnect(ar);
                    success();
                }
                catch (Exception e)
                {
                    exception(e);
                }
            }, null);
        }

        // Receive method is called by the client to iterate messages recv from the server
        // and parse it into a Message object.
        public IEnumerable<Message> Receive()
        {
            while (true)
            {
                // read length header
                byte[] lengthHeader = new byte[4];
                int readLength = _socket.Receive(lengthHeader);
                if (readLength == 0)
                {
                    yield break;
                }
                int length = BitConverter.ToInt32(lengthHeader, 0);

                // read variable length data
                byte[] data = new byte[length];
                int recvedLen = 0;
                while (recvedLen < length)
                {
                    readLength = _socket.Receive(data, recvedLen, length - recvedLen, SocketFlags.None);
                    if (readLength == 0)
                    {
                        yield break;
                    }
                    recvedLen += readLength;
                }
                // decrypt data by aes with gcm mode
                byte[] decryptedData = _aes.Decrypt(data);

                // yield parsed messagepack data
                yield return MessagePackSerializer.Deserialize<Message>(decryptedData);
            }
        }

        // get next message from iterator
        public Message? ReceiveNext()
        {
            IEnumerator<Message> iter = Receive().GetEnumerator();
            if (iter.MoveNext())
            {
                return iter.Current;
            }
            return null;
        }

        public void Send(Message message)
        {
            Logger.Debug($"> {this} {message}");
            byte[] data = MessagePackSerializer.Serialize(message);
            byte[] encryptedData = _aes.Encrypt(data);
            byte[] lengthHeader = BitConverter.GetBytes(encryptedData.Length);
            _socket.Send(lengthHeader);
            _socket.Send(encryptedData);
        }

        public int ReceiveTimeout
        {
            get { return _socket.ReceiveTimeout; }
            set { _socket.ReceiveTimeout = value; }
        }

        public void Shutdown()
        {
            _socket.Shutdown(SocketShutdown.Both);
        }

        public void Dispose()
        {
            _socket.Dispose();
            _aes.Dispose();
        }

        public override string ToString()
        {
            return $"{host}:{port}";
        }

        public void UpdateAesKey(byte[] key)
        {
            _aes = new Aes256Gcm(key);
        }
    }
}
