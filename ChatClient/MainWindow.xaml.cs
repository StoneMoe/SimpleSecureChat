using ChatClient;
using ChatClient.Model;
using ChatClient.Visual;
using Common.Cipher;
using Common.Network;
using SQLite;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;

namespace SSC_Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        TCPClient m_tcp;
        string m_nick = "";

        UIState m_UIState;
        IntPtr m_wpfHwnd;

        SQLiteConnection m_db;

        readonly string m_WndTitle = "Simple Secure Chat";

        public enum UIState
        {
            Idle,
            Connected,
            Disconnecting,
            Connecting
        }


        #region Events
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            Environment.Exit(0);
        }

        private void messageArea_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            this.messageArea.ScrollToEnd();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //flash notice init
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);
            m_wpfHwnd = wndHelper.Handle;

            //UI reset
            SetUI(UIState.Idle);
            ClearMessageArea();

            //db init
            m_db = new SQLiteConnection("trusted.db");
            m_db.CreateTable<Server>();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            Sendmsg();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine(System.Windows.Markup.XamlWriter.Save(messageArea.Document));
                FileStream fs = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\ChatLog_" + DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss") + ".html", FileMode.Append);

                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                sw.Write(System.Windows.Markup.XamlWriter.Save(messageArea.Document).Replace("<Paragraph", "<br><span").Replace("</Paragraph>", "</span>"));
                sw.Close();
                fs.Close();
                AddStatus("Chat log saved to Desktop");
            }
            catch (Exception)
            {
                AddStatus("Chat log save failed");
            }

        }

        private void SendBox_PreviewKeyDown(object sender, KeyEventArgs e)
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

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            //Disconnect
            if (m_UIState == UIState.Connected)
            {
                DoDisconnect(autoIdle: false);
                return;
            }

            //Connect
            string host;
            int port;
            try
            {
                NetUtils.Resolve(HostBox.Text);
                host = HostBox.Text;
                port = NetUtils.ResolvePort(PortBox.Text);
            }
            catch (Exception)
            {
                AddStatus("Invalid Host or Port");
                return;
            }
            if (KeyBox.Text.Length == 0)
            {
                AddStatus("Key is empty");
                return;
            }

            m_nick = NickBox.Text.Trim();
            if (m_nick == "")
            {
                AddStatus("Nick is empty");
                return;
            }

            //Go
            DoConnect(host, port);
        }

        #endregion

        #region UI Helper
        public void ClearMessageArea()
        {
            messageArea.Document.Blocks.Clear();
        }
        public void SetSendUI(bool enabled)
        {
            this.Dispatcher.Invoke(new Action(() =>
                {
                    SendButton.IsEnabled = enabled;
                    sendBox.IsEnabled = enabled;
                }));
        }

        public void SetUI(UIState flag)
        {
            m_UIState = flag;
            this.Dispatcher.Invoke(new Action(() =>
                {
                    switch (flag)
                    {
                        case UIState.Idle:
                            //Title
                            TitleTextBlock.Text = m_WndTitle;

                            //UI
                            ConnectButton.Content = "Connect";
                            ConnectButton.IsEnabled = true;

                            HostBox.IsEnabled = true;
                            PortBox.IsEnabled = true;
                            NickBox.IsEnabled = true;
                            KeyBox.IsEnabled = true;

                            messageArea.Width = sendBox.Width = 460;
                            ConnectButton.Margin = new Thickness(479, 275, 0, 0);

                            SetSendUI(false);

                            break;
                        case UIState.Connected:
                            //Title
                            TitleTextBlock.Text = string.Format("{0} @ {1} : {2}", m_nick, HostBox.Text, PortBox.Text);

                            //UI
                            ConnectButton.Content = "Disconnect";
                            ConnectButton.IsEnabled = true;

                            HostBox.IsEnabled = false;
                            PortBox.IsEnabled = false;
                            NickBox.IsEnabled = false;
                            KeyBox.IsEnabled = false;

                            messageArea.Width = 660;
                            sendBox.Width = 660;
                            ConnectButton.Margin = new Thickness(479, 435, 0, 0);

                            SetSendUI(true);

                            break;
                        case UIState.Connecting:
                        case UIState.Disconnecting:
                            ConnectButton.Content = "Wait...";
                            ConnectButton.IsEnabled = false;

                            HostBox.IsEnabled = false;
                            PortBox.IsEnabled = false;
                            NickBox.IsEnabled = false;
                            KeyBox.IsEnabled = false;

                            SetSendUI(false);
                            break;
                        default:
                            throw new NotImplementedException("Unknown UI State");
                    }
                }));
        }

        public void AddMessage(string name, string msg, bool self = false)
        {
            //@ detect
            bool at = false;
            if (msg.Contains("@" + m_nick))
            {
                at = true;
            }

            //Add to message area
            this.Dispatcher.Invoke(new Action(() =>
            {

                Run fl = new Run(string.Format("{0} {1}", name, DateTime.Now.ToString("hh:mm:ss")));
                Run r = new Run(msg);

                if (self)
                {
                    //add Name part
                    Paragraph paragraph = new Paragraph();
                    paragraph.Margin = new Thickness(3, 6, 0, 3);
                    paragraph.Foreground = Colors.Green;
                    paragraph.Inlines.Add(fl);
                    messageArea.Document.Blocks.Add(paragraph);

                    //add msg part
                    paragraph = new Paragraph();
                    paragraph.Margin = new Thickness(3, 0, 0, 6);
                    paragraph.Inlines.Add(r);
                    messageArea.Document.Blocks.Add(paragraph);
                }
                else
                {
                    //add Name part
                    Paragraph paragraph = new Paragraph();
                    paragraph.Margin = new Thickness(3, 6, 0, 3);
                    paragraph.Foreground = Colors.DeepBlue;
                    paragraph.Inlines.Add(fl);
                    messageArea.Document.Blocks.Add(paragraph);

                    //add msg part
                    paragraph = new Paragraph();
                    paragraph.Margin = new Thickness(3, 0, 0, 6);
                    if (at)
                    {
                        r.Background = Colors.CyanBlue;
                    }
                    paragraph.Inlines.Add(r);
                    messageArea.Document.Blocks.Add(paragraph);
                }

                //flash notice
                if (!SSC_Window.IsFocused)
                {
                    FlashTaskBar(m_wpfHwnd, falshType.FLASHW_TIMERNOFG);
                }
            }));

        }

        public void AddSvrMsg(string msg)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                Paragraph paragraph = new Paragraph();
                Run r = new Run(msg);
                paragraph.Margin = new Thickness(3, 3, 0, 0);
                paragraph.Foreground = Colors.SoftRed;
                paragraph.FontStyle = FontStyles.Italic;
                paragraph.Inlines.Add(r);
                messageArea.Document.Blocks.Add(paragraph);

                //flash notice
                if (!SSC_Window.IsFocused)
                {
                    FlashTaskBar(m_wpfHwnd, falshType.FLASHW_TIMERNOFG);
                }
            }));
        }


        public void AddStatus(string msg)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                Paragraph paragraph = new Paragraph();
                Run r = new Run(msg);
                paragraph.Margin = new Thickness(3, 3, 0, 0);
                paragraph.Foreground = Colors.Gray;
                paragraph.FontStyle = FontStyles.Italic;
                paragraph.Inlines.Add(r);
                messageArea.Document.Blocks.Add(paragraph);

                //flash notice
                if (!SSC_Window.IsFocused)
                {
                    FlashTaskBar(m_wpfHwnd, falshType.FLASHW_TIMERNOFG);
                }
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

        public enum falshType : uint
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

        public static bool FlashTaskBar(IntPtr hWnd, falshType type)
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

        public void DoDisconnect(bool autoIdle = true)
        {
            SetUI(UIState.Disconnecting);
            m_tcp.Shutdown();
            if (autoIdle)
            {
                AddStatus("Disconnected");
                SetUI(UIState.Idle);
            }
        }

        public void DoConnect(string host, int port)
        {
            SetUI(UIState.Connecting);
            AddStatus("Connecting...");
            m_tcp = new(host, port, KeyBox.Text);
            m_tcp.ReceiveTimeout = 3000;  // will set to 0 after setup
            m_tcp.BeginConnect(() =>
            {
                Task.Run(SetupWorker);
            },
            (exc) =>
            {
                AddStatus($"Connect Failed ({exc.Message})");
                SetUI(UIState.Idle);
            });
        }

        ManualResetEvent m_fpWndCloseEvent = new ManualResetEvent(false);
        FingerPrint fpWnd;

        public void SetupWorker() // session setup after connected
        {

            Message? msg;
            X509Certificate2 serverCert;

            //Client hello for ECC public key
            m_tcp.Send(new(MsgType.HELLO));
            msg = m_tcp.ReceiveNext();
            if (msg == null || msg.Type != MsgType.HELLO)
            {
                AddStatus("Session Setup Failed");
                DoDisconnect();
                return;
            }

            try
            {
                var certData = msg.GetParam<byte[]>(0);
                serverCert = X509.Load(certData);
            }
            catch (Exception)
            {
                AddStatus("Invalid Certificate");
                DoDisconnect();
                return;
            }

            m_fpWndCloseEvent.Reset();
            Dispatcher.Invoke(() =>
            {
                fpWnd = new FingerPrint(this, serverCert);
                fpWnd.Closed += (object? sender, EventArgs e) =>
                {
                    if (fpWnd.trustResult == FingerPrint.TrustResult.Trust)
                    {
                        m_db.Insert(new Server() { Host = m_tcp.host, Port = m_tcp.port, SHA1 = serverCert.GetCertHashString() });
                    }
                };

                if (m_db.Query<Server>("SELECT * FROM Server WHERE Host = ? AND SHA1 = ?", m_tcp.host, serverCert.GetCertHashString()).Count == 0)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        fpWnd.ShowDialog();
                    }));
                }
                else
                {
                    fpWnd.trustResult = FingerPrint.TrustResult.Trust;
                }
                m_fpWndCloseEvent.Set();
            });
            m_fpWndCloseEvent.WaitOne();

            if (fpWnd.trustResult == FingerPrint.TrustResult.Cancel)
            {
                AddStatus("User Canceled");
                DoDisconnect();
                return;
            }

            //ECDH for session key
            try
            {
                ECDH ecdh = new();
                m_tcp.Send(new(MsgType.KEY, ecdh.GetPublicKeyBlob()));
                byte[] newKey = ecdh.DeriveKey(serverCert);
                m_tcp.UpdateAesKey(newKey);

                //key exchange check
                msg = m_tcp.ReceiveNext();
                if (msg != null && msg.Type != MsgType.KEY)
                {
                    AddStatus("Key Exchange Failed");
                    DoDisconnect();
                    return;
                }
            }
            catch (Exception ex)
            {
                AddStatus("Key Exchange Failed");
                AddStatus(ex.ToString());
                DoDisconnect();
                return;
            }

            //acquire nickname
            try
            {
                m_tcp.Send(new(MsgType.NICK, m_nick));
            }
            catch (Exception ex)
            {
                AddStatus("Nickname Acquire Failed");
                AddStatus(ex.ToString());
                DoDisconnect();
                return;
            }

            //MsgLoop
            MsgLoop();
        }

        public void MsgLoop()
        {
            try
            {
                foreach (Message msg in m_tcp.Receive())
                {
                    switch (msg.Type)
                    {
                        case MsgType.NICK:
                            if (msg.GetParam<string>(0) == "OK")
                            {
                                m_tcp.ReceiveTimeout = 0;
                                AddStatus("Connected");
                                SetUI(UIState.Connected);
                            }
                            else
                            {
                                AddSvrMsg(msg.GetParam<string>(0));
                                DoDisconnect();
                                return;
                            }
                            break;
                        case MsgType.MSG:
                            AddMessage(msg.GetParam<string>(0), msg.GetParam<string>(1));
                            break;
                        case MsgType.SYS:
                            AddSvrMsg(msg.GetParam<string>(0));
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                AddStatus(ex.ToString());
            }
            finally
            {
                if (m_UIState != UIState.Disconnecting)
                {
                    AddStatus("Connection Lost");
                }
                AddStatus("Disconnected");
                SetUI(UIState.Idle);
            }
        }

        public void Sendmsg()
        {
            if (sendBox.Text == "") return;

            try
            {
                m_tcp.Send(new(MsgType.MSG, sendBox.Text));
                AddMessage(m_nick, sendBox.Text, self: true);
                sendBox.Text = "";
            }
            catch
            {
                //ignore
            }
        }

    }
}