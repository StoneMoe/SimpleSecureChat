using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net;

namespace ChatServer
{
    internal class Server
    {
        Random ra = new Random();
        Socket mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        string listenAddr = "0.0.0.0:12344";
        bool isShuttingDown = false;
        bool shouldBroadcastJoinLeave = true;
        string AESKey = "SSCv3*Default_AES@Key&1234567890";
        string BotName = "Server";
        string WelcomeMsg = "Welcome!";
        public void Log(string text)
        {
            var time = DateTime.Now;
            Console.WriteLine(string.Format("[{0}] {1}", time, text));
        }

        #region Data Helpers

        public string Encrypt(string toEncrypt, string key)
        {
            try
            {
                byte[] keyArray = Encoding.UTF8.GetBytes(key);
                byte[] toEncryptArray = Encoding.UTF8.GetBytes(toEncrypt);

                RijndaelManaged rDel = new RijndaelManaged();
                rDel.Key = keyArray;
                rDel.Mode = CipherMode.ECB;
                rDel.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = rDel.CreateEncryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                return Convert.ToBase64String(resultArray, 0, resultArray.Length);
            }
            catch
            {
                Log("AES Encrypt Error");
                return "Server Error";
            }
        }

        public string Decrypt(string toDecrypt, string key)
        {
            try
            {
                byte[] keyArray = Encoding.UTF8.GetBytes(key);
                byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);

                RijndaelManaged rDel = new RijndaelManaged();
                rDel.Key = keyArray;
                rDel.Mode = CipherMode.ECB;
                rDel.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = rDel.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                return Encoding.UTF8.GetString(resultArray);
            }
            catch
            {
                Log("AES Decrypt Error");
                return "Server Error";
            }
        }

        public string[] parseData(string rawdata)
        {
            string first = Decrypt(rawdata, AESKey);
            string[] tmpsplit = first.Split(new[] { "|" }, StringSplitOptions.None);
            string[] result = new string[2];
            result[0] = tmpsplit[0];
            result[1] = Decrypt(tmpsplit[1], AESKey);
            return result;
        }

        public byte[] parseMsg(string cmd, string msg)
        {
            return str2bytes(Encrypt(string.Format("{0}|{1}", cmd, Encrypt(msg, AESKey)), AESKey) + "\r\n");
        }

        public byte[] str2bytes(string before)
        {
            return Encoding.UTF8.GetBytes(before);
        }
        #endregion

        public Server()
        {
            IPAddress ip = IPAddress.Any;
            try
            {
                ip = IPAddress.Parse(listenAddr.Split(':')[0]);
            }
            catch (Exception)
            {
                Log("Listen IP Invalid");
                Environment.Exit(0);
            }

            int port = Convert.ToInt32(listenAddr.Split(':')[1]);
            if (port > 65535 || port < 1)
            {
                Log("Listen Port Invalid");
                Environment.Exit(0);
            }

            if (!new[] { 16, 24, 32 }.Contains(AESKey.Length) || Encoding.UTF8.GetByteCount(AESKey) != AESKey.Length)
            {
                Log("AES Key must be 16/24/32 bytes long and must be ASCII encoded");
                Environment.Exit(0);
            }

            Log("Creating socket");


            try
            {
                IPEndPoint iep = new IPEndPoint(ip, port);
                mainSocket.Bind(iep);
                mainSocket.Listen(1024);
            }
            catch (SocketException)
            {
                Log("Port " + port.ToString() + " may be used");
                return;
            }
            catch (OutOfMemoryException)
            {
                Log("Out of Memory");
                return;
            }
            catch (Exception ex)
            {
                Log("Unknown Exception\r\n" + ex.Message);
                return;
            }
        }

        public void Dispose()
        {
            Log("Server is shutting down");
            isShuttingDown = true;
            Log("Closing MainSocket");
            mainSocket.Dispose();
            Log("Waiting for Accept Handler finish its work");
            while (true)
            {
                Thread.Sleep(1500);
                if (!isShuttingDown)
                {
                    Log("Server stoped");
                    break;
                }
                else
                {
                    Log("Server is still shutting down");
                }

            }
            return;
        }
        public void Loop()
        {
            Log("Server now listening on: " + mainSocket.LocalEndPoint.ToString());
            try
            {
                while (true)
                {
                    Socket client = mainSocket.Accept();
                    Log(string.Format("{0} - Connected", client.RemoteEndPoint.ToString()));

                    Thread t = new Thread(() =>
                    {
                        ClientHandler(client);
                    });
                    t.IsBackground = true;
                    t.Start();
                }
            }
            catch (SocketException)
            {
                if (isShuttingDown)
                {
                    ClientMgr.kickAllClient();
                    isShuttingDown = false;
                    return;
                }
                Log("Accept Handler Socket Exception");
                Log("new client may can't connect anymore,but server will try to keep current user still online");
                return;
            }
            catch (OutOfMemoryException)
            {
                Log("Out of Memory");
                return;
            }
            catch (Exception ex)
            {
                Log("Unknown Exception\r\n" + ex.Message);
                return;
            }
        }

        public void ClientHandler(Socket client)
        {
            byte[] buffer;
            int recvLength;

            //protocol
            bool NicknameSeted = false;
            string nickname = "";
            client.ReceiveTimeout = 15000; //not ready client drop interval 15s

            string addr = client.RemoteEndPoint.ToString();

            //network
            bool alreadyDisposed = false;

            //Handler
            try
            {
                while (true)
                {
                    buffer = new byte[102400]; //flush buffer
                    recvLength = client.Receive(buffer);
                    if (recvLength != 0)
                    {
                        string tmp = Encoding.UTF8.GetString(buffer, 0, recvLength);
                        string[] data = parseData(tmp);

                        //Nick command
                        if (data[0] == "NICK")
                        {
                            //username is set?
                            if (NicknameSeted)
                            {
                                client.Send(parseMsg("INFO", "ALREADY_SET"));
                                Log(string.Format("{0} - Set nickname - {1} - {2}", client.RemoteEndPoint.ToString(), data[1], "Failed(ALREADY_SET)"));
                                continue; //next while
                            }

                            //is username existed?
                            if (ClientMgr.ClientNicknameExisted(data[1]))
                            {
                                client.Send(parseMsg("INFO", "NICKNAME_EXIST"));
                                Log(string.Format("{0} - Set nickname - {1} - {2}", client.RemoteEndPoint.ToString(), data[1], "Failed(NICKNAME_EXIST)"));
                                alreadyDisposed = true;
                                client.Dispose();
                                continue;
                            }

                            //is username allowed?
                            if (data[1][0] == ('&') || data[1][0] == ('@') || data[1].Contains(" ") || data[1].Contains(":") || data[1].Trim() == string.Empty)  //@ is server msg prefix , & is admin user msg prefix
                            {
                                client.Send(parseMsg("INFO", "NICKNAME_NOT_ALLOW"));
                                Log(string.Format("{0} - Set nickname - {1} - {2}", client.RemoteEndPoint.ToString(), data[1], "Failed(NICKNAME_NOT_ALLOW)"));
                                alreadyDisposed = true;
                                client.Dispose();
                                continue;
                            }

                            //All green! Let's set nickname now

                            ClientMgr.Register(data[1], client); //register socket and nickname to Dictionary.
                            nickname = data[1].Replace("\\", "\\\\"); //save nickname to current session
                            NicknameSeted = true; // turn nickname set process off
                            client.ReceiveTimeout = 0; //set infinite timeout

                            client.Send(parseMsg("INFO", "OK"));

                            Log(string.Format("{0} - Set nickname - {1} - {2}", client.RemoteEndPoint.ToString(), data[1], "OK"));

                            //send welcome msg to client
                            if (WelcomeMsg != "")
                            {
                                client.Send(parseMsg("MSG", string.Format("@{0}:{1}", BotName, WelcomeMsg)));
                            }
                            //Broadcast join msg
                            if (shouldBroadcastJoinLeave)
                            {
                                Broadcast(parseMsg("MSG", string.Format("@{0}:{1}", BotName, "\"" + nickname + "\" Joined.")), true);
                            }

                            continue;
                        }


                        //Msg command
                        if (data[0] == "MSG")
                        {
                            //username is set?
                            if (!NicknameSeted)
                            {
                                client.Send(parseMsg("INFO", "NICKNAME_NOT_SET"));
                                Log(string.Format("{0} - Send Msg - {1} - {2}", client.RemoteEndPoint.ToString(), data[1], "Failed(NICKNAME_NOT_SET)"));
                                continue;
                            }
                            //All green! Let's do something with this message

                            Log(string.Format("{0} ({1}): {2}", client.RemoteEndPoint.ToString(), nickname, data[1]));

                            string[] commandstring = data[1].Split(' ', StringSplitOptions.None);

                            //is this msg a command?
                            if (data[1] == "@h")
                            {
                                client.Send(parseMsg("MSG", string.Format("@{0}:{1}", BotName, "Commands: \r\n@h - Show Help\r\n@l - List Online Users\r\n@m [User] [Msg] - send a private msg")));
                                continue;
                            }

                            if (data[1] == "@l")
                            {
                                client.Send(parseMsg("MSG", string.Format("@{0}:{1}", BotName, "Online:\r\n " + ClientMgr.fmtAllClients())));
                                continue;
                            }

                            if (commandstring[0] == "@m")
                            {
                                if (commandstring.Length < 3)
                                {
                                    client.Send(parseMsg("MSG", string.Format("@{0}:{1}", BotName, "Usage: @m [User] [Message]")));
                                    continue;
                                }
                                string TargetUser = commandstring[1];

                                if (!ClientMgr.clientMap.ContainsKey(TargetUser))
                                {
                                    client.Send(parseMsg("MSG", string.Format("@{0}:{1}", BotName, "User \"" + TargetUser + "\" Not Found")));
                                    continue;
                                }
                                else
                                {
                                    string _tmpmsg = "";
                                    for (int i = 2; i < commandstring.Length; i++)
                                    {
                                        _tmpmsg += commandstring[i] + " ";
                                    }
                                    ClientMgr.clientMap[TargetUser].Send(parseMsg("MSG", string.Format("@{0}:{1}", "PM From " + nickname, _tmpmsg)));
                                    continue;
                                }
                            }

                            //broadcast to any other client if this is a normal msg
                            Broadcast(parseMsg("MSG", string.Format("{0}:{1}", nickname, data[1])), false, client);

                        }
                    }
                    else
                    {
                        //Recv size == 0

                        if (NicknameSeted)
                        {
                            ClientMgr.Unregister(nickname);
                        }

                        if (!alreadyDisposed)
                        {
                            alreadyDisposed = true;
                            client.Dispose();
                        }


                        //leaved msg
                        if (shouldBroadcastJoinLeave && NicknameSeted)
                        {
                            Broadcast(parseMsg("MSG", string.Format("@{0}:{1}", BotName, "\"" + nickname + "\" Leaved.")), true);
                        }

                        Log("Disconnect due to recving 0 byte");
                        Log(string.Format("{0} ({1}) - Disconnected", addr, nickname));
                        addr = null;

                        //exit endless while
                        break;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                if (NicknameSeted)
                {
                    ClientMgr.Unregister(nickname);
                }

                if (!alreadyDisposed)
                {
                    alreadyDisposed = true;
                    client.Dispose();
                }

                //leaved msg
                if (shouldBroadcastJoinLeave && NicknameSeted)
                {
                    Broadcast(parseMsg("MSG", string.Format("@{0}:{1}", BotName, "\"" + nickname + "\" Leaved.")), true);
                }

                Log(string.Format("{0} - Unknown - {1} - {2}", addr, "Unknown", "Failed(Unexpected MSG Format/AES Key Error)"));
                Log("Disconnect due to IndexOutOfRangeException");
                Log(string.Format("{0} ({1}) - Disconnected", addr, nickname));
                addr = null;
            }
            catch (SocketException)
            {
                if (NicknameSeted)
                {
                    ClientMgr.Unregister(nickname);
                }

                if (!alreadyDisposed)
                {
                    alreadyDisposed = true;
                    client.Dispose();
                }

                //leaved msg
                if (shouldBroadcastJoinLeave && NicknameSeted)
                {
                    Broadcast(parseMsg("MSG", string.Format("@{0}:{1}", BotName, "\"" + nickname + "\" Leaved.")), true);
                }
                Log("Disconnect due to SocketException");
                Log(string.Format("{0} ({1}) - Disconnected", addr, nickname));
                addr = null;
            }
            catch (ObjectDisposedException)
            {
                if (NicknameSeted)
                {
                    ClientMgr.Unregister(nickname);
                }

                if (!alreadyDisposed)
                {
                    alreadyDisposed = true;
                    client.Dispose();
                }

                //leaved msg
                if (shouldBroadcastJoinLeave && NicknameSeted)
                {
                    Broadcast(parseMsg("MSG", string.Format("@{0}:{1}", BotName, "\"" + nickname + "\" Leaved.")), true);
                }
                Log("Disconnect due to ObjectDisposedException");
                Log(string.Format("{0} ({1}) - Disconnected", addr, nickname));
                addr = null;
            }
            catch (Exception ex)
            {
                if (NicknameSeted)
                {
                    ClientMgr.Unregister(nickname);
                }

                if (!alreadyDisposed)
                {
                    alreadyDisposed = true;
                    client.Dispose();
                }

                //leaved msg
                if (shouldBroadcastJoinLeave && NicknameSeted)
                {
                    Broadcast(parseMsg("MSG", string.Format("@{0}:{1}", BotName, "\"" + nickname + "\" Leaved.")), true);
                }
                Log(string.Format("{0} - Unknown - {1} - {2}", addr, "Unknown", "Failed(Unknown Exception)"));
                Log(string.Format("Exception Info: {0}", ex.ToString()));
                Log("Disconnect due to Unknown Exception");
                Log(string.Format("{0} ({1}) - Disconnected", addr, nickname));
                addr = null;
            }
        }

        public void Broadcast(byte[] msg, bool ForALL, Socket exceptClient = null)
        {
            try
            {
                if (ForALL)
                {
                    foreach (var s in ClientMgr.clientMap)
                    {
                        s.Value.Send(msg);
                    }
                }
                else
                {
                    foreach (var s in ClientMgr.clientMap)
                    {
                        if (s.Value != exceptClient)
                        {
                            s.Value.Send(msg);
                        }
                    }
                }
            }
            catch { }
        }
    }
}
