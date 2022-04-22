using Common.Log;
using Common.Network;

namespace ChatServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TCPServer tcp = new("0.0.0.0", 12344, "SSCv3_Default_Key");

            tcp.Listen((exc) =>
            {
                Logger.Info("Listen failed ({0})", exc.Message);
                Environment.Exit(-1);
            });

            tcp.AcceptLoop(
                (client) => Logger.Info("{0} Connected", client),
                (client) => {
                    Logger.Info("{0} Disconnected", client);
                    ClientMgr.Unregister(client);
                    client.Dispose();
                },
                (client, msg) => MessageHandler(client, msg),
                (socket, client, exc) =>
                {
                    if (socket != null && client != null)
                    {
                        Logger.Info("Error from {0} ({1})", client, exc.Message);
                    }
                    else
                    {
                        Logger.Info("Error on accept ({0})", exc.Message);
                    }
                }
            );

            tcp.Dispose();
            Logger.Info("Server stopped");
        }

        static void MessageHandler(TCPClient client, Message msg)
        {
            string logStr = $"{client} {msg.Type} - ";
            var log = (string result) =>
            {
                Logger.Info(logStr + result);
            };

            bool nickReady = client.Storage.ContainsKey("nick");
            string nick = (string)client.Storage.GetValueOrDefault("nick", "");

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
                    client.Storage.Add("nick", pendingNick);
                    ClientMgr.Register(pendingNick, client);
                    client.ReceiveTimeout = 0;

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
                case MsgType.SYS:
                    Logger.Error("{0} sending SYS({1}) which is not support on server side", client, msg.Params);
                    break;
                case MsgType.HELLO:
                    break;
                case MsgType.KEY:
                    break;
                default:
                    Logger.Error("{0} sending unknown message type({1})", client, msg.Type);
                    break;
            }
        }
    }
}
