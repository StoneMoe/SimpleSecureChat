using Common.Network.Data;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace Common.Network.Transport
{
    // This is the interface for the transport layer
    // Implementations of this interface are responsible for sending and receiving data only
    // and provide reliable message-oriented async network operate via the Send and Receive methods
    // but underlying detail is up to the implementation
    public interface ITransport<T> : IDisposable
    {
        public int ConnectTimeout { get; set; }
        public int SendTimeout { get; set; }
        public int ReceiveTimeout { get; set; }
        public string RemoteHost { get; }
        public int RemotePort { get; }
        public string LocalHost { get; }
        public int LocalPort { get; }


        public void Connect(string host, int port, Action success, Action<Exception> exception);
        public void Disconnect();
        public bool IsConnected();


        public void Start(string ip, int port);
        public void Stop();
        public ITransport<T> Accept();


        public void Send(T data);
        public IEnumerable<T> Receive();
    }
}
