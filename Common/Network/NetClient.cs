using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using Common.Cipher;
using MessagePack;
using System.Net;
using Common.Log;
using Common.Network.Data;
using Common.Network.Transport;

namespace Common.Network
{
    public class NetClient : IDisposable
    {
        public string RemoteHost { get => transport.RemoteHost; }
        public int RemotePort { get => transport.RemotePort; }

        public Dictionary<string, object> state = new();

        public readonly ITransport<byte[]> transport;
        Aes256Gcm _aes;

        public NetClient(string aesPwd)
        {
            transport = new TCPTransport();
            _aes = new Aes256Gcm(aesPwd);
        }

        public NetClient(ITransport<byte[]> clientSocket, Aes256Gcm aesInstance)
        {
            transport = clientSocket;
            _aes = aesInstance;
        }

        public void UpdateAesKey(byte[] key)
        {
            _aes = new Aes256Gcm(key);
        }

        // Connect is called by the client to initiate the connection to the server asynchronously.
        // success delegate is called when the connection is established.
        // error delegate is called if the connection fails.
        public void Connect(string host, int port, Action success, Action<Exception> exception)
        {
            transport.Connect(host, port, success, exception);
        }

        // Receive method is called by the client to iterate messages recv from the server
        // and parse it into a Message object.
        public IEnumerable<Message> Receive()
        {
            foreach (byte[] cipherData in transport.Receive())
            {
                // decrypt data
                byte[] decryptedData = _aes.Decrypt(cipherData);
                // yield parsed messagepack data
                yield return (Message)decryptedData;
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
            byte[] encryptedData = _aes.Encrypt((byte[])message);
            transport.Send(encryptedData);
        }

        public void Disconnect()
        {
            transport.Disconnect();
        }

        public void Dispose()
        {
            transport.Dispose();
            _aes.Dispose();
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            return $"{RemoteHost}:{RemotePort}";
        }
    }
}
