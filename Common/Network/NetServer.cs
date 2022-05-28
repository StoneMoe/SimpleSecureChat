using System;
using System.Net.Sockets;
using System.Net;
using Common.Cipher;
using System.Threading.Tasks;
using Common.Log;
using Common.Network.Data;
using Common.Network.Transport;

namespace Common.Network
{

    public class NetServer : IDisposable
    {
        public readonly ITransport<byte[]> transport;
        readonly Aes256Gcm _aes;

        public NetServer(string aesPwd)
        {
            transport = new TCPTransport();
            _aes = new Aes256Gcm(aesPwd);
        }

        public void Listen(string ip, int port, Action<Exception> excHandler)
        {
            try
            {
                transport.Start(ip, port);
                Logger.Info("Listening at {0}:{1}", ip, port);
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
            Action<NetClient> connectHandler, Action<NetClient> disconnectHandler,
            Action<NetClient, Message> msgHandler, Action<ITransport<byte[]>?, NetClient?, Exception> excHandler)
        {
            Logger.Info("Accepting connections...");
            while (true)
            {
                ITransport<byte[]>? clientTransport = null;
                NetClient? secureClient = null;
                try
                {
                    clientTransport = transport.Accept();
                    secureClient = new(clientTransport, _aes.Copy());
                    connectHandler(secureClient);
                }
                catch (Exception ex)
                {
                    excHandler(clientTransport, secureClient, ex);
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
                        excHandler(clientTransport, secureClient, ex);
                    }
                    finally
                    {
                        try
                        {
                            disconnectHandler(secureClient);
                        }
                        catch (Exception ex)
                        {
                            excHandler(clientTransport, secureClient, ex);
                        }
                    }
                });
            }
        }

        public void Dispose()
        {
            transport.Dispose();
            _aes.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
