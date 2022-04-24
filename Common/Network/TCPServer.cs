using System;
using System.Net.Sockets;
using System.Net;
using Common.Cipher;
using System.Threading.Tasks;
using Common.Log;

namespace Common.Network
{

    public class TCPServer : IDisposable
    {
        readonly Aes256Gcm _aes;

        string _ip;
        int _port;
        Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public TCPServer(string ip, int port, string aesPwd)
        {
            _ip = ip;
            _port = port;
            _aes = new Aes256Gcm(aesPwd);
        }

        public void Listen(Action<Exception> excHandler)
        {
            try
            {
                _socket.Bind(new IPEndPoint(IPAddress.Parse(_ip), _port));
                _socket.Listen(1024);
                Logger.Info("Listening at {0}:{1}", _ip, _port);
            }
            catch (Exception ex)
            {
                excHandler(ex);
            }
        }

        /// <summary>
        /// Accepts a new connection and processing with delegates
        /// </summary>
        /// <param name="connectHandler">called when client connected</param>
        /// <param name="disconnectHandler">called when client disconnected</param>
        /// <param name="msgHandler">called when new message recved</param>
        /// <param name="excHandler">called when any exception throwed</param>
        public void AcceptLoop(
            Action<TCPClient> connectHandler, Action<TCPClient> disconnectHandler,
            Action<TCPClient, Message> msgHandler, Action<Socket?, TCPClient?, Exception> excHandler)
        {
            Logger.Info("Accepting connections...");
            while (true)
            {
                Socket? clientSocket = null;
                TCPClient? secureClient = null;
                try
                {
                    clientSocket = _socket.Accept();
                    secureClient = new(clientSocket, _aes.Copy());
                    connectHandler(secureClient);
                }
                catch (Exception ex)
                {
                    excHandler(clientSocket, secureClient, ex);
                    break;
                }

                // client thread
                Task.Run(() =>
                {
                    try
                    {
                        foreach (Message msg in secureClient.Receive())
                        {
                            Logger.Debug($"< {secureClient} {msg}");
                            msgHandler(secureClient, msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        excHandler(clientSocket, secureClient, ex);
                    }
                    finally
                    {
                        try
                        {
                            disconnectHandler(secureClient);
                        }
                        catch (Exception ex)
                        {
                            excHandler(clientSocket, secureClient, ex);
                        }
                    }
                });
            }
        }

        public void Dispose()
        {
            _socket.Close();
            _socket.Dispose();
            _aes.Dispose();
        }
    }
}
