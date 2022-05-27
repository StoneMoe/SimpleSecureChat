using Common.Cipher;
using Common.Log;
using Common.Network;
using Common.Network.Data;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ChatServer
{
    internal class Program
    {
#pragma warning disable CS8618
        static X509Certificate2 serverCert;
#pragma warning restore CS8618

        static void Main()
        {
            try
            {
                serverCert = X509.LoadDefault();
            }
            catch
            {
                Logger.Error("Failed to load certificate, generating self-signed certificate...");
                X509.GenerateDefault();
                Environment.Exit(1);
            }

            if (serverCert == null)
            {
                Logger.Error("Failed to load certificate");
                Environment.Exit(1);
            }

            if (serverCert.GetECDsaPrivateKey() == null)
            {
                Logger.Error("Invalid ECDSA PKCS#12 Certificate");
                Environment.Exit(1);
            }
            else
            {
                Logger.Info($"Loaded X.509 {serverCert.GetCertHashString()}");
            }

            NetServer server = new("SSCv3_Default_Key");

            server.Listen("0.0.0.0", 12344, (exc) =>
            {
                Logger.Error("Listen failed ({0})", exc.Message);
                Environment.Exit(1);
            });

            server.AcceptLoop(
                (client) => {
                    Logger.Info("{0} Connected", client);
                    client.transport.ReceiveTimeout = 30000;
                },
                (client) =>
                {
                    Logger.Info("{0} Disconnected", client);
                    ClientMgr.Unregister(client);
                    client.Dispose();
                },
                (client, msg) => MessageHandler(client, msg),
                (socket, client, exc) =>
                {
                    if (socket != null && client != null)
                    {
                        Logger.Error($"{client} Error occured. \n{exc})");
                    }
                    else
                    {
                        Logger.Error("Error on accept ({0})", exc.Message);
                    }
                }
            );

            server.Dispose();
            Logger.Info("Server stopped");
        }

        static void MessageHandler(NetClient client, Message msg)
        {
            string logStr = $"{client} {msg.Type} - ";
            var log = (string result) =>
            {
                Logger.Info(logStr + result);
            };

            bool nickReady = client.state.ContainsKey("nick");
            string nick = (string)client.state.GetValueOrDefault("nick", "");

            switch (msg.Type)
            {
                case MsgType.NICK:
                    string pendingNick = msg.GetParam<string>(0);
                    //if nickname already set
                    if (nickReady)
                    {
                        client.Send(new(MsgType.NICK, "Nickname already set"));
                        log("Failed(ALREADY_SET)");
                        return;
                    }

                    //if username duplicate
                    if (ClientMgr.ClientNicknameExisted(pendingNick))
                    {
                        client.Send(new(MsgType.NICK, "Nickname existed"));
                        log("Failed(DUPLICATE)");
                        return;
                    }

                    //if username not allowed
                    if (new char[] { '&', '@' }.Contains(pendingNick[0]))
                    {
                        client.Send(new(MsgType.NICK, "Illegal nickname"));
                        log("Failed(ILLEGAL)");
                        return;
                    }

                    //setup nickname
                    client.state.Add("nick", pendingNick);
                    ClientMgr.Register(pendingNick, client);
                    client.transport.ReceiveTimeout = 0;

                    //reply
                    client.Send(new(MsgType.NICK, "OK"));
                    log("OK");

                    //send welcome msg to client
                    client.Send(new(MsgType.SYS, $"Welcome to server."));

                    //broadcast join msg
                    ClientMgr.Broadcast(new(MsgType.SYS, $"{pendingNick} joined."));
                    break;
                case MsgType.MSG:
                    string text = msg.GetParam<string>(0);
                    //if username not set
                    if (!nickReady)
                    {
                        client.Send(new(MsgType.SYS, "Nickname not set"));
                        log("Failed(NICKNAME_NOT_SET)");
                        return;
                    }
                    //log
                    Logger.Info($"{client} ({nick}): {text}");

                    //command handling
                    string[] commandstring = text.Split(' ', StringSplitOptions.None);

                    //is this msg a command?
                    if (text == "@h")
                    {
                        client.Send(new(MsgType.MSG, "Server", "Commands: \r\n@h - Show Help\r\n@l - List Online Users\r\n@m [User] [Msg] - send a private msg"));
                        return;
                    }

                    if (text == "@l")
                    {
                        client.Send(new(MsgType.MSG, "Server", $"Online:\r\n{ClientMgr.FmtAllClients()}"));
                        return;
                    }

                    if (commandstring[0] == "@m")
                    {
                        if (commandstring.Length < 3)
                        {
                            client.Send(new(MsgType.MSG, "Server", "Usage: @m [User] [Message]"));
                            return;
                        }
                        string TargetUser = commandstring[1];

                        if (!ClientMgr.clientMap.ContainsKey(TargetUser))
                        {
                            client.Send(new(MsgType.MSG, "Server", $"User \"{TargetUser}\" Not Found"));
                            return;
                        }
                        else
                        {
                            string _tmpmsg = "";
                            for (int i = 2; i < commandstring.Length; i++)
                            {
                                _tmpmsg += commandstring[i] + " ";
                            }
                            ClientMgr.clientMap[TargetUser].Send(new(MsgType.MSG, $"PM From {nick}", _tmpmsg));
                            return;
                        }
                    }

                    //broadcast to any other client
                    ClientMgr.Broadcast(new(MsgType.MSG, nick, text), client);

                    break;
                case MsgType.HELLO:
                    client.Send(new(MsgType.HELLO, serverCert.Export(X509ContentType.Cert)));
                    break;
                case MsgType.KEY:
                    ECDH ecdh = new(serverCert);
                    byte[] keyData = msg.GetParam<byte[]>(0);
                    byte[]? newKey = ecdh.DeriveKey(keyData);
                    client.UpdateAesKey(newKey);
                    client.Send(new(MsgType.KEY));
                    break;
                default:
                    Logger.Error("{0} sending unknown message type({1})", client, msg.Type);
                    break;
            }
        }
    }
}
