using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.Text;

namespace SSC_Server
{
    public partial class MainWindow : Form
    {
        Random ra = new Random();

        bool inDebugMode = false;

        Socket mainSocket;

        bool isShuttingDown = false;

        string key;

        #region Events Function
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            SYSLog("Program Start", true);
        }
        private void LogBox_TextChanged(object sender, EventArgs e)
        {
            //Auto Scroll
            LogBox.SelectionStart = LogBox.Text.Length;
            LogBox.ScrollToCaret();
        }
        #endregion

        #region UI Helper
        public void makeUI(bool enable)
        {
            this.Invoke(new Action(() =>
            {
                if (enable)
                {
                    ListenBox.Enabled = true;
                    AESbox.Enabled = true;

                    StartButton.Text = "Start Server";
                }
                else
                {
                    ListenBox.Enabled = false;
                    AESbox.Enabled = false;

                    StartButton.Text = "Stop Server";
                }

            }));
        }
        #endregion

        #region Log Helper

        public void SYSLog(string text, bool firstline = false)
        {
            this.Invoke(new Action(() =>
            {
                if (firstline)
                {
                    LogBox.Text += string.Format("[{0} ({1})] [{2}]: {3}", DateTime.Now.ToString(), TimeZoneInfo.Local.ToString().Split(')')[0].Split('(')[1], "SYS", text);
                    return;
                }
                LogBox.Text += string.Format("\r\n[{0} ({1})] [{2}]: {3}", DateTime.Now.ToString(), TimeZoneInfo.Local.ToString().Split(')')[0].Split('(')[1], "SYS", text);
            }));
        }

        public void MSGLog(string text, bool firstline = false)
        {
            this.Invoke(new Action(() =>
            {
                if (firstline)
                {
                    LogBox.Text += string.Format("[{0} ({1})] [{2}]: {3}", DateTime.Now.ToString(), TimeZoneInfo.Local.ToString().Split(')')[0].Split('(')[1], "MSG", text);
                    return;
                }
                LogBox.Text += string.Format("\r\n[{0} ({1})] [{2}]: {3}", DateTime.Now.ToString(), TimeZoneInfo.Local.ToString().Split(')')[0].Split('(')[1], "MSG", text);
            }));
        }

        public void ERRLog(string text, bool firstline = false)
        {

            this.Invoke(new Action(() =>
            {
                if (firstline)
                {
                    LogBox.Text += string.Format("[{0} ({1})] [{2}]: {3}", DateTime.Now.ToString(), TimeZoneInfo.Local.ToString().Split(')')[0].Split('(')[1], "ERR", text);
                    return;
                }
                LogBox.Text += string.Format("\r\n[{0} ({1})] [{2}]: {3}", DateTime.Now.ToString(), TimeZoneInfo.Local.ToString().Split(')')[0].Split('(')[1], "ERR", text);
            }));
        }

        public void debugLog(string text, bool firstline = false)
        {
            if (!inDebugMode)
            {
                return;
            }
            this.Invoke(new Action(() =>
            {
                if (firstline)
                {
                    LogBox.Text += string.Format("[{0} ({1})] [{2}]: {3}", DateTime.Now.ToString(), TimeZoneInfo.Local.ToString().Split(')')[0].Split('(')[1], "DEBUG", text);
                    return;
                }
                LogBox.Text += string.Format("\r\n[{0} ({1})] [{2}]: {3}", DateTime.Now.ToString(), TimeZoneInfo.Local.ToString().Split(')')[0].Split('(')[1], "DEBUG", text);
            }));
        }

        #endregion

        #region Client Dictionary
        public Dictionary<string, Socket> clientDic = new Dictionary<string, Socket>();

        public void RegClient(string name, Socket s)
        {
            clientDic.Add(name, s);
        }

        public void UnRegClient(string name)
        {
            clientDic.Remove(name);
        }

        public bool ClientNicknameExisted(string username)
        {
            foreach (var s in clientDic)
            {
                if (s.Key == username)
                {
                    return true;
                }
            }
            return false;
        }

        public void kickAllClient()
        {
            try
            {
                foreach (var item in clientDic)
                {
                    item.Value.Dispose();
                }
            }
            catch { }

        }

        public string outputAllClient()
        {
            string temp = "";
            foreach (var s in clientDic)
            {
                temp += string.Format("\"{0}\" ", s.Key);
            }
            return temp;
        }
        #endregion

        #region Data Helpers

        public string Encrypt(string toEncrypt, string key)
        {
            try
            {
                byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
                byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

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
                debugLog("AES Encrypt Error");
                return "Server Error";
            }
        }

        public string Decrypt(string toDecrypt, string key)
        {
            try
            {
                byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
                byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);

                RijndaelManaged rDel = new RijndaelManaged();
                rDel.Key = keyArray;
                rDel.Mode = CipherMode.ECB;
                rDel.Padding = PaddingMode.PKCS7;

                ICryptoTransform cTransform = rDel.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                return UTF8Encoding.UTF8.GetString(resultArray);
            }
            catch
            {
                debugLog("AES Decrypt Error");
                return "Server Error";
            }
        }

        public string[] parseData(string rawdata)
        {
            string first = Decrypt(rawdata, key);
            string[] tmpsplit = first.Split(new String[] { "|" }, StringSplitOptions.None);
            string[] result = new string[2];
            result[0] = tmpsplit[0];
            result[1] = Decrypt(tmpsplit[1], key);
            return result;
        }

        public byte[] parseMsg(string cmd, string msg)
        {
            return str2bytes(Encrypt(string.Format("{0}|{1}", cmd, Encrypt(msg, key)), key) + "\r\n");
        }

        public byte[] str2bytes(string before)
        {
            return Encoding.UTF8.GetBytes(before);
        }
        #endregion

        #region Network

        private void StartButton_Click(object sender, EventArgs e)
        {
            if (StartButton.Text == "Stop Server")
            {
                Thread t = new Thread(() =>
                {
                    SYSLog("Server is shutting down");
                    isShuttingDown = true;
                    this.Invoke(new Action(() =>
                    {
                        StartButton.Enabled = false;
                    }));


                    debugLog("Closing MainSocket");
                    mainSocket.Dispose();

                    debugLog("Waiting for Accept Handler finish its work");
                    while (true)
                    {
                        Thread.Sleep(1500);
                        if (!isShuttingDown)
                        {
                            SYSLog("Server stoped");
                            this.Invoke(new Action(() =>
                            {
                                StartButton.Enabled = true;
                            }));
                            makeUI(true);
                            break;
                        }
                        else
                        {
                            SYSLog("Server is still shutting down");
                        }

                    }
                    //free key
                    key = null;
                    return;
                });

                t.IsBackground = true;
                t.Start();
                return;
            }

            debugLog("Server in debug mode now(Just output more infomation in console)");

            debugLog("Checking config");
            try
            {
                IPAddress.Parse(ListenBox.Text.Split(':')[0]);
            }
            catch (Exception)
            {
                ERRLog("Listen IP Invalid");
                return;
            }

            if (Convert.ToInt32(ListenBox.Text.Split(':')[1]) > 65535 || Convert.ToInt32(ListenBox.Text.Split(':')[1]) < 1)
            {
                ERRLog("Listen Port Invalid");
                return;
            }

            if (!(AESbox.Text.Length == 16 || AESbox.Text.Length == 24 || AESbox.Text.Length == 32))
            {
                ERRLog("AES Key must be 16/24/32 bytes");
                return;
            }


            debugLog("Starting worker");
            makeUI(false);

            debugLog("Loading AES Key");
            key = AESbox.Text;

            debugLog("Loading MainSocket");
            IPAddress ip = IPAddress.Parse(ListenBox.Text.Split(':')[0]);
            int port = Convert.ToInt32(ListenBox.Text.Split(':')[1]);


            mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                IPEndPoint iep = new IPEndPoint(ip, port);
                mainSocket.Bind(iep);
                mainSocket.Listen(1024);

                debugLog("Starting Accept Handler thread");
                Thread t = new Thread(() =>
                {
                    AcceptHandler();
                });

                t.IsBackground = true;
                t.Start();
            }
            catch (SocketException)
            {
                makeUI(true);
                ERRLog("Port " + port.ToString() + " may be used");
                return;
            }
            catch (OutOfMemoryException)
            {
                makeUI(true);
                ERRLog("Out of Memory");
                return;
            }
            catch (Exception ex)
            {
                makeUI(true);
                ERRLog("Unknown Exception\r\n" + ex.Message);
                return;
            }
        }

        public void AcceptHandler()
        {
            SYSLog("Server now listening on: " + mainSocket.LocalEndPoint.ToString());
            try
            {
                while (true)
                {
                    Socket client = mainSocket.Accept();
                    SYSLog(string.Format("{0} - Connected", client.RemoteEndPoint.ToString()));

                    debugLog("Starting new Client Handler thread");
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
                    kickAllClient();
                    isShuttingDown = false;
                    return;
                }
                makeUI(true);
                ERRLog("Accept Handler Socket Exception");
                ERRLog("new client may can't connect anymore,but server will try to keep current user still online");
                return;
            }
            catch (OutOfMemoryException)
            {
                makeUI(true);
                ERRLog("Out of Memory");
                return;
            }
            catch (Exception ex)
            {
                makeUI(true);
                ERRLog("Unknown Exception\r\n" + ex.Message);
                return;
            }
        }

        public void ClientHandler(Socket client)
        {
            /*
             * msg format: AES([command]|AES([data]))
             */
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
                                SYSLog(string.Format("{0} - Set nickname - {1} - {2}", client.RemoteEndPoint.ToString(), data[1], "Failed(ALREADY_SET)"));
                                continue; //next while
                            }

                            //is username existed?
                            if (ClientNicknameExisted(data[1]))
                            {
                                client.Send(parseMsg("INFO", "NICKNAME_EXIST"));
                                SYSLog(string.Format("{0} - Set nickname - {1} - {2}", client.RemoteEndPoint.ToString(), data[1], "Failed(NICKNAME_EXIST)"));
                                alreadyDisposed = true;
                                client.Dispose();
                                continue;
                            }

                            //is username allowed?
                            if (data[1][0] == ('&') || data[1][0] == ('@') || data[1].Contains(" ") || data[1].Contains(":") || data[1].Trim() == string.Empty)  //@ is server msg prefix , & is admin user msg prefix
                            {
                                client.Send(parseMsg("INFO", "NICKNAME_NOT_ALLOW"));
                                SYSLog(string.Format("{0} - Set nickname - {1} - {2}", client.RemoteEndPoint.ToString(), data[1], "Failed(NICKNAME_NOT_ALLOW)"));
                                alreadyDisposed = true;
                                client.Dispose();
                                continue;
                            }

                            //All green! Let's set nickname now

                            RegClient(data[1], client); //register socket and nickname to Dictionary.
                            nickname = data[1].Replace("\\", "\\\\"); //save nickname to current session
                            NicknameSeted = true; // turn nickname set process off
                            client.ReceiveTimeout = 0; //set infinite timeout

                            client.Send(parseMsg("INFO", "OK"));

                            SYSLog(string.Format("{0} - Set nickname - {1} - {2}", client.RemoteEndPoint.ToString(), data[1], "OK"));

                            //send welcome msg to client
                            if (WelcomeMsgBox.Checked)
                            {
                                client.Send(parseMsg("MSG", string.Format("@{0}:{1}", ServerNameBox.Text, WelcomeMsg.Text)));
                            }
                            //Broadcast join msg
                            if (broadcastBox.Checked)
                            {
                                Broadcast(parseMsg("MSG", string.Format("@{0}:{1}", ServerNameBox.Text, "\"" + nickname + "\" Joined.")), true);
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
                                SYSLog(string.Format("{0} - Send Msg - {1} - {2}", client.RemoteEndPoint.ToString(), data[1], "Failed(NICKNAME_NOT_SET)"));
                                continue;
                            }
                            //All green! Let's do something with this message

                            MSGLog(string.Format("{0} ({1}): {2}", client.RemoteEndPoint.ToString(), nickname, data[1]));

                            string[] commandstring = data[1].Split(new String[] { " " }, StringSplitOptions.None);

                            //is this msg a command?
                            if (data[1] == "@h")
                            {
                                client.Send(parseMsg("MSG", string.Format("@{0}:{1}", ServerNameBox.Text, "Commands: \r\n@h - Show Help\r\n@l - List Online Users\r\n@m [User] [Msg] - send a private msg")));
                                continue;
                            }

                            if (data[1] == "@l")
                            {
                                client.Send(parseMsg("MSG", string.Format("@{0}:{1}", ServerNameBox.Text, "Online:\r\n " + outputAllClient())));
                                continue;
                            }

                            if (commandstring[0] == "@m")
                            {
                                if (commandstring.Length < 3)
                                {
                                    client.Send(parseMsg("MSG", string.Format("@{0}:{1}", ServerNameBox.Text, "Usage: @m [User] [Message]")));
                                    continue;
                                }
                                string TargetUser = commandstring[1];

                                if (!clientDic.ContainsKey(TargetUser))
                                {
                                    client.Send(parseMsg("MSG", string.Format("@{0}:{1}", ServerNameBox.Text, "User \"" + TargetUser + "\" Not Found")));
                                    continue;
                                }
                                else
                                {
                                    string _tmpmsg = "";
                                    for (int i = 2; i < commandstring.Length; i++)
                                    {
                                        _tmpmsg += commandstring[i] + " ";
                                    }
                                    clientDic[TargetUser].Send(parseMsg("MSG", string.Format("@{0}:{1}", "PM From " + nickname, _tmpmsg)));
                                    continue;
                                }
                            }

                            //broadcast to any other client if this is a normal msg
                            Broadcast(parseMsg("MSG", string.Format("{0}:{1}", nickname, data[1])), false, client);

                        }
                    }
                    else
                    {
                        //may be lost connection 1
                        //Recv size == 0

                        if (NicknameSeted)
                        {
                            UnRegClient(nickname);
                        }

                        if (!alreadyDisposed)
                        {
                            alreadyDisposed = true;
                            client.Dispose();
                        }


                        //leaved msg
                        if (broadcastBox.Checked && NicknameSeted)
                        {
                            Broadcast(parseMsg("MSG", string.Format("@{0}:{1}", ServerNameBox.Text, "\"" + nickname + "\" Leaved.")), true);
                        }

                        debugLog("This is No.1 Disconnect Point.");
                        SYSLog(string.Format("{0} ({1}) - Disconnected", addr, nickname));
                        addr = null;

                        //exit endless while
                        break;
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                //may be lost connection 2
                if (NicknameSeted)
                {
                    UnRegClient(nickname);
                }

                if (!alreadyDisposed)
                {
                    alreadyDisposed = true;
                    client.Dispose();
                }

                //leaved msg
                if (broadcastBox.Checked && NicknameSeted)
                {
                    Broadcast(parseMsg("MSG", string.Format("@{0}:{1}", ServerNameBox.Text, "\"" + nickname + "\" Leaved.")), true);
                }

                ERRLog(string.Format("{0} - Unknown - {1} - {2}", addr, "Unknown", "Failed(Unexpected MSG Format/AES Key Error)"));
                debugLog("This is No.2 Disconnect Point.");
                SYSLog(string.Format("{0} ({1}) - Disconnected", addr, nickname));
                addr = null;
            }
            catch (SocketException)
            {
                //may be lost connection 3
                if (NicknameSeted)
                {
                    UnRegClient(nickname);
                }

                if (!alreadyDisposed)
                {
                    alreadyDisposed = true;
                    client.Dispose();
                }

                //leaved msg
                if (broadcastBox.Checked && NicknameSeted)
                {
                    Broadcast(parseMsg("MSG", string.Format("@{0}:{1}", ServerNameBox.Text, "\"" + nickname + "\" Leaved.")), true);
                }
                debugLog("This is No.3 Disconnect Point.");
                SYSLog(string.Format("{0} ({1}) - Disconnected", addr, nickname));
                addr = null;
            }
            catch (ObjectDisposedException)
            {
                //may be lost connection 4
                if (NicknameSeted)
                {
                    UnRegClient(nickname);
                }

                if (!alreadyDisposed)
                {
                    alreadyDisposed = true;
                    client.Dispose();
                }

                //leaved msg
                if (broadcastBox.Checked && NicknameSeted)
                {
                    Broadcast(parseMsg("MSG", string.Format("@{0}:{1}", ServerNameBox.Text, "\"" + nickname + "\" Leaved.")), true);
                }
                debugLog("This is No.4 Disconnect Point.");
                SYSLog(string.Format("{0} ({1}) - Disconnected", addr, nickname));
                addr = null;
            }
            catch (Exception ex)
            {
                //may be lost connection 5
                if (NicknameSeted)
                {
                    UnRegClient(nickname);
                }

                if (!alreadyDisposed)
                {
                    alreadyDisposed = true;
                    client.Dispose();
                }

                //leaved msg
                if (broadcastBox.Checked && NicknameSeted)
                {
                    Broadcast(parseMsg("MSG", string.Format("@{0}:{1}", ServerNameBox.Text, "\"" + nickname + "\" Leaved.")), true);
                }
                ERRLog(string.Format("{0} - Unknown - {1} - {2}", addr, "Unknown", "Failed(Unknown Exception)"));
                ERRLog(string.Format("Exception Info: {0}", ex.ToString()));
                debugLog("This is No.5 Disconnect Point.");
                SYSLog(string.Format("{0} ({1}) - Disconnected", addr, nickname));
                addr = null;
            }
        }

        public void Broadcast(byte[] msg, bool ForALL, Socket exceptClient = null)
        {
            try
            {
                if (ForALL)
                {
                    foreach (var s in clientDic)
                    {
                        s.Value.Send(msg);
                    }
                }
                else
                {
                    foreach (var s in clientDic)
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

        #endregion
    }
}
