using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Net.Sockets;
using System.Net;
using System.Windows.Documents;
using System.Windows.Input;
using System.Threading;
using System.Windows.Media;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace SSC_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Socket s;

        string nickname;

        bool isDisconnecting;

        string key;

        Thread presetThread;

        Thread recvThread;


        IntPtr wpfHwnd;




        #region Events
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void messageArea_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.messageArea.ScrollToEnd();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //flash init
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            wpfHwnd = wndHelper.Handle;

            //UI reset
            messageArea.Document.Blocks.Clear();
            makeSend(false);
            makeConnect(1);
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            Sendmsg();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine(System.Windows.Markup.XamlWriter.Save(messageArea.Document));
                FileStream fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\ChatLog_" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss") + ".html", FileMode.Append);

                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                sw.Write(System.Windows.Markup.XamlWriter.Save(messageArea.Document).Replace("<Paragraph", "<br><span").Replace("</Paragraph>", "</span>"));
                sw.Close();
                fs.Close();
                addLog("Chat log saved to Desktop");
            }
            catch (Exception)
            {
                addLog("Chat log save failed");
            }

        }

        private void sendBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Enter)
            {
                e.Handled = true;
                Sendmsg();
                sendBox.Text = "";
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
            {
                e.Handled = true;
                sendBox.SelectedText = Environment.NewLine;
                sendBox.Select(sendBox.SelectionStart + 1, 0);
            }
        }

        #endregion

        #region Data Helper
        public string Encrypt(string toEncrypt, string key)
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

        public string Decrypt(string toDecrypt, string key)
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
            return str2bytes(Encrypt(string.Format("{0}|{1}", cmd, Encrypt(msg, key)), key));
        }

        public byte[] str2bytes(string before)
        {
            return Encoding.UTF8.GetBytes(before);
        }

        #endregion

        #region UI Helper

        public void makeSend(bool on)
        {
            this.Dispatcher.Invoke(new Action(() =>
                {
                    SendButton.IsEnabled = on;
                    sendBox.IsEnabled = on;
                }));
        }

        public void makeConnect(int flag)
        {
            this.Dispatcher.Invoke(new Action(() =>
                {
                    switch (flag)
                    {
                        case 1:
                            ConnectButton.Content = "Connect";
                            ConnectButton.IsEnabled = true;
                            IPBox.IsEnabled = true;
                            PortBox.IsEnabled = true;
                            NickBox.IsEnabled = true;
                            KeyBox.IsEnabled = true;

                            messageArea.Width = 460;
                            sendBox.Width = 460;
                            ConnectButton.Margin = new Thickness(479,334,0,0);
                            break;
                        case 2:
                            ConnectButton.Content = "Disconnect";
                            ConnectButton.IsEnabled = true;
                            IPBox.IsEnabled = false;
                            PortBox.IsEnabled = false;
                            NickBox.IsEnabled = false;
                            KeyBox.IsEnabled = false;

                            messageArea.Width = 660;
                            sendBox.Width = 660;
                            ConnectButton.Margin = new Thickness(479, 435, 0, 0);
                            break;
                        case 3:
                            ConnectButton.Content = "Wait...";
                            ConnectButton.IsEnabled = false;
                            IPBox.IsEnabled = false;
                            PortBox.IsEnabled = false;
                            NickBox.IsEnabled = false;
                            KeyBox.IsEnabled = false;
                            break;
                        default:
                            break;
                    }
                }));

        }

        public void AddMessage(string msg, bool self = false)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                
                string _name = msg.Substring(0, msg.IndexOf(':', 0, msg.Length));
                string _msg = msg.Substring(msg.IndexOf(':', 0, msg.Length) + 1);
                Run fl = new Run(string.Format("{0} {1}", _name, DateTime.Now.ToString("hh:mm:ss")));
                Run r = new Run(_msg);
                if (self)
                {
                    Paragraph paragraph = new Paragraph();
                    paragraph.Margin = new Thickness(3, 6, 0, 3);
                    paragraph.Foreground = new SolidColorBrush(Color.FromRgb(51, 153, 102));
                    paragraph.Inlines.Add(fl);
                    messageArea.Document.Blocks.Add(paragraph);
                    paragraph = new Paragraph();
                    paragraph.Margin = new Thickness(3, 0, 0, 6);
                    paragraph.Inlines.Add(r);
                    messageArea.Document.Blocks.Add(paragraph);
                    if (!SSC_Window.IsFocused)
                    {
                        flashTaskBar(wpfHwnd, falshType.FLASHW_TIMERNOFG);
                    }
                    return;
                }
                Paragraph paragrapha = new Paragraph();
                paragrapha.Margin = new Thickness(3, 6, 0, 3);
                paragrapha.Foreground = new SolidColorBrush(Color.FromRgb(0, 102, 225));
                paragrapha.Inlines.Add(fl);
                messageArea.Document.Blocks.Add(paragrapha);
                paragrapha = new Paragraph();
                paragrapha.Margin = new Thickness(3, 0, 0, 6);
                paragrapha.Inlines.Add(r);
                messageArea.Document.Blocks.Add(paragrapha);
                if (!SSC_Window.IsFocused)
                {
                    flashTaskBar(wpfHwnd, falshType.FLASHW_TIMERNOFG);
                }
                return;
            }));
        }

        public void addLog(string msg)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                Paragraph paragraph = new Paragraph();
                Run r = new Run(msg);
                paragraph.Margin = new Thickness(3, 3, 0, 0);
                paragraph.Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100));
                paragraph.Inlines.Add(r);
                messageArea.Document.Blocks.Add(paragraph);
                flashTaskBar(wpfHwnd, falshType.FLASHW_TIMERNOFG);
            }));
        }
        
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        [DllImport("user32.dll")]
        public static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        public enum falshType:uint
        {
            FLASHW_STOP = 0,    //停止闪烁
            FALSHW_CAPTION = 1,  //只闪烁标题
            FLASHW_TRAY = 2,   //只闪烁任务栏
            FLASHW_ALL = 3,     //标题和任务栏同时闪烁
            FLASHW_PARAM1 = 4,
            FLASHW_PARAM2 = 12,
            FLASHW_TIMER = FLASHW_TRAY | FLASHW_PARAM1,   //无条件闪烁任务栏直到发送停止标志，停止后高亮
            FLASHW_TIMERNOFG = FLASHW_TRAY | FLASHW_PARAM2  //未激活时闪烁任务栏直到发送停止标志或者窗体被激活，停止后高亮
        }

        public static bool flashTaskBar(IntPtr hWnd, falshType type)
        {
            FLASHWINFO fInfo = new FLASHWINFO();
            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = hWnd;
            fInfo.dwFlags = (uint)type;
            fInfo.uCount = Convert.ToUInt32(5);
            fInfo.dwTimeout = 1000; //窗口闪烁的频度，毫秒为单位；若该值为0，则为默认图标的闪烁频度
            return FlashWindowEx(ref fInfo);
        }

        #endregion

        #region Network

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if ((string)ConnectButton.Content == "Disconnect")
            {
                makeConnect(3);

                Thread t = new Thread(() =>
                {
                    isDisconnecting = true;
                    s.Dispose();

                    while (true)
                    {
                        Thread.Sleep(1500);
                        if (!isDisconnecting)
                        {
                            addLog("Disconnected");
                            makeConnect(1);
                            break;
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

            IPAddress ip;
            int port;

            if (!IPAddress.TryParse(IPBox.Text,out ip))
            {
                try
                {
                    ip = Dns.GetHostEntry(IPBox.Text).AddressList[0];
                    Console.WriteLine(ip.ToString());
                }
                catch
                {
                    addLog("Host IP/Domain Invalid");
                    return;
                }

            }

            if (Convert.ToInt32(PortBox.Text) > 65535 || Convert.ToInt32(PortBox.Text) < 1)
            {
                addLog("Port Invalid");
                return;
            }
            else
            {
                port = Convert.ToInt32(PortBox.Text);
            }


            if (!(KeyBox.Text.Length == 16 || KeyBox.Text.Length == 24 || KeyBox.Text.Length == 32))
            {
                addLog("AES Key must be either 16/24/32 bytes");
                return;
            }
            else
            {
                key = KeyBox.Text;
            }



            if (NickBox.Text.Trim() == "")
            {
                addLog("Nickname is nessesary");
                return;
            }
            else
            {
                nickname = NickBox.Text.Trim();
            }
            //All Green and start connect
            makeConnect(3);

            isDisconnecting = false;

            ConnectHandler(ip,port);
        }

        public void ConnectHandler(IPAddress ip, int port)
        {
            try
            {
                addLog("Connecting to Host...");
                IPEndPoint iep = new IPEndPoint(ip, port);

                s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                s.Connect(new IPEndPoint(ip, Convert.ToInt32(PortBox.Text)));
                addLog("Connected");
                presetThread = new Thread(() =>
                {
                    presetHandler();
                });
                presetThread.IsBackground = true;
                presetThread.Start();
            }
            catch (SocketException)
            {
                s.Dispose();
                key = null;
                addLog("Can't reach Host");
                makeConnect(1);
            }
            catch (OutOfMemoryException)
            {
                s.Dispose();
                key = null;
                addLog("Out of Memory");
                makeConnect(1);
            }
            catch (Exception ex)
            {
                s.Dispose();
                key = null;
                addLog("Unknown Exception Occured");
                MessageBox.Show(ex.Message);
                makeConnect(1);
            }
        }

        public void presetHandler() //set nickname etc.
        {
            try
            {
                addLog("Setting Nickname to \"" + nickname + "\"...");
                byte[] buffer = new byte[102400];
                Thread temp = new Thread(() => {
                    Thread.Sleep(500);
                    s.Send(parseMsg("NICK", nickname));
                });
                temp.IsBackground = true;
                temp.Start();
                s.ReceiveTimeout = 3000;
                int recvLength = s.Receive(buffer);
                if (recvLength != 0)
                {
                    string tmp = Encoding.UTF8.GetString(buffer, 0, recvLength);
                    string[] data = parseData(tmp);

                    if (data[0] == "INFO" && data[1] == "OK")
                    {
                        addLog("Nickname set succeed");
                        s.ReceiveTimeout = 0;
                        makeConnect(2);
                        makeSend(true);

                        recvThread = new Thread(() =>
                        {
                            RecvHandler();
                        });

                        recvThread.IsBackground = true;
                        recvThread.Start();
                    }
                    else
                    {
                        key = null;
                        addLog(data[1]);
                        makeConnect(1);
                        return;
                    }
                }
                else
                {
                    key = null;
                    addLog("Failed,Maybe Connection issue or AES Key mismatch (Err Code 1)");
                    makeConnect(1);
                    return;
                }
            }
            catch (SocketException)
            {
                key = null;
                addLog("Failed,Maybe Connection issue or AES Key mismatch (Err Code 2)");
                makeConnect(1);
                return;
            }
            catch (Exception ex)
            {
                key = null;
                addLog("Failed,Maybe SSC protocol version mismatch");
                MessageBox.Show(ex.Message);
                makeConnect(1);
            }
        }

        public void RecvHandler()
        {
            byte[] recvbuffer;
            string[] data;

            try
            {
                while (true)
                {
                    recvbuffer = new byte[102400];
                    int receiveLength = s.Receive(recvbuffer);
                    if (receiveLength != 0)
                    {
                        string tmp = Encoding.UTF8.GetString(recvbuffer, 0, receiveLength);
                        //strip packet
                        string[] packets = tmp.Split(new String[] { "\r\n" }, StringSplitOptions.None);
                        for (int i = 0; i < packets.Length; i++)
                        {
                            if (packets[i] == "")
                            {
                                continue;
                            }
                            data = parseData(packets[i]);
                            if (data[0] == "MSG")
                            {
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    AddMessage(data[1]);
                                }));
                            }
                        }
                    }
                    else
                    {
                        if (!isDisconnecting)
                        {
                            addLog("Lost connection with Host");
                            makeConnect(1);
                        }
                        else
                        {
                            isDisconnecting = false;
                        }
                        makeSend(false);
                        return;
                    }
                }
            }
            catch (SocketException)
            {
                if (!isDisconnecting)
                {
                    addLog("Lost connection with Host");
                    makeConnect(1);
                }
                else
                {
                    isDisconnecting = false;
                }
                makeSend(false);
                return;
            }
            catch (Exception ex)
            {
                if (!isDisconnecting)
                {
                    addLog("Unknown Exception Occured");
                    MessageBox.Show(ex.ToString());
                    makeConnect(1);
                }
                else
                {
                    isDisconnecting = false;
                }
                makeSend(false);
                return;
            }
        }

        public void Sendmsg()
        {
            //Send Button
            try
            {
                if (sendBox.Text == "")
                {
                    return;
                }
                s.Send(parseMsg("MSG", sendBox.Text));

                AddMessage(string.Format("{0}:{1}", nickname, sendBox.Text),true);
                sendBox.Text = "";
            }
            catch (Exception)
            {
                if (!isDisconnecting)
                {
                    addLog("Lost connection with Host,Your last message may lose");
                    makeConnect(1);
                }
                else
                {
                    isDisconnecting = false;
                }
                makeSend(false);
                return;
            }
        }

        

        #endregion

        

    }
}