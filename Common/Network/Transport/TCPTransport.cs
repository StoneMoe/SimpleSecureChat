using Common.Network.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Common.Network.Transport
{
    public class TCPTransport : ITransport<byte[]>
    {
        readonly Socket m_tcp;

        public int ConnectTimeout { get; set; } = 5000;
        public int SendTimeout { get => m_tcp.SendTimeout; set => m_tcp.SendTimeout = value; }
        public int ReceiveTimeout { get => m_tcp.ReceiveTimeout; set => m_tcp.ReceiveTimeout = value; }

        public string RemoteHost => m_tcp.RemoteEndPoint is IPEndPoint ep ? ep.Address.ToString() : throw new NullReferenceException();
        public int RemotePort => m_tcp.RemoteEndPoint is IPEndPoint ep ? ep.Port : throw new NullReferenceException();
        public string LocalHost => m_tcp.LocalEndPoint is IPEndPoint ep ? ep.Address.ToString() : throw new NullReferenceException();
        public int LocalPort => m_tcp.LocalEndPoint is IPEndPoint ep ? ep.Port : throw new NullReferenceException();

        public TCPTransport()
        {
            m_tcp = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }
        public TCPTransport(Socket socket)
        {
            m_tcp = socket;
        }

        public void Connect(string host, int port, Action success, Action<Exception> exception)
        {
            var ar = m_tcp.BeginConnect(host, port, null, null);
            bool connected = ar.AsyncWaitHandle.WaitOne(ConnectTimeout, true);
            if (connected)
            {
                try
                {
                    m_tcp.EndConnect(ar);
                    success();
                }
                catch (Exception e)
                {
                    exception(e);
                }
            }
            else
            {
                m_tcp.Dispose();
                exception(new SocketException(10060)); // WSAETIMEDOUT 10060
            }
        }

        public void Disconnect()
        {
            m_tcp.Shutdown(SocketShutdown.Both);
        }

        public bool IsConnected()
        {
            return m_tcp.Connected;
        }

        public void Start(string ip, int port)
        {
            m_tcp.Bind(new IPEndPoint(IPAddress.Parse(ip), port));
            m_tcp.Listen(1024);
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public ITransport<byte[]> Accept()
        {
            return new TCPTransport(m_tcp.Accept());
        }
        public void Send(byte[] data)
        {
            byte[] lengthHeader = BitConverter.GetBytes(data.Length);
            m_tcp.Send(lengthHeader);
            m_tcp.Send(data);
        }

        public IEnumerable<byte[]> Receive()
        {
            while (true)
            {
                // read length header
                byte[] lengthHeader = new byte[4];
                int readLength = m_tcp.Receive(lengthHeader);
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
                    readLength = m_tcp.Receive(data, recvedLen, length - recvedLen, SocketFlags.None);
                    if (readLength == 0)
                    {
                        yield break;
                    }
                    recvedLen += readLength;
                }
                yield return data;
            }
        }

        public void Dispose()
        {
            m_tcp.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
