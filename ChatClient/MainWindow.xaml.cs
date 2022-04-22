using ChatClient.Visual;
using Common.Network;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        string m_nick;

        UIState m_UIState;
        IntPtr m_wpfHwnd;

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
                AddLog("Chat log saved to Desktop");
            }
            catch (Exception)
            {
                AddLog("Chat log save failed");
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
                DoDisconnect();
                return;
            }

            //Connect
            IPAddress ip;
            int port;
            try
            {
                ip = Utils.Resolve(IPBox.Text);
                port = Utils.ResolvePort(PortBox.Text);
            }
            catch (Exception)
            {
                AddLog("Invalid IP or Port");
                return;
            }
            if (KeyBox.Text.Length == 0)
            {
                AddLog("Key is empty");
                return;
            }

            m_nick = NickBox.Text.Trim();
            if (m_nick == "")
            {
                AddLog("Nick is empty");
                return;
            }

            //Go
            DoConnect(ip, port);
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

                            IPBox.IsEnabled = true;
                            PortBox.IsEnabled = true;
                            NickBox.IsEnabled = true;
                            KeyBox.IsEnabled = true;

                            messageArea.Width = sendBox.Width = 460;
                            ConnectButton.Margin = new Thickness(479, 275, 0, 0);

                            SetSendUI(false);

                            break;
                        case UIState.Connected:
                            //Title
                            TitleTextBlock.Text = string.Format("{0} @ {1} : {2}", m_nick, IPBox.Text, PortBox.Text);

                            //UI
                            ConnectButton.Content = "Disconnect";
                            ConnectButton.IsEnabled = true;

                            IPBox.IsEnabled = false;
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

                            IPBox.IsEnabled = false;
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

        public void AddLog(string msg)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                Paragraph paragraph = new Paragraph();
                Run r = new Run(msg);
                paragraph.Margin = new Thickness(3, 3, 0, 0);
                paragraph.Foreground = Colors.SoftRed;
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

        public void DoDisconnect()
        {
            SetUI(UIState.Disconnecting);
            m_tcp.Shutdown();
        }

        public void DoConnect(IPAddress ip, int port)
        {
            SetUI(UIState.Connecting);
            AddLog("Connecting to Host...");
            m_tcp = new(ip, port, KeyBox.Text);
            m_tcp.ReceiveTimeout = 3000;  // will set to 0 after setup
            m_tcp.BeginConnect(() =>
            {
                AddLog("Connected");
                Task.Run(MsgLoop);
                Task.Run(SetupWorker);
            },
            (exc) =>
            {
                AddLog($"Connect Failed ({exc.Message})");
                m_tcp.Dispose();
                SetUI(UIState.Idle);
            });
        }

        public void SetupWorker() // session setup after connected
        {
            AddLog($"Setting nickname to \"{m_nick}\"...");
            m_tcp.Send(new(MsgType.NICK, new[] { m_nick }));
        }

        public void MsgLoop()
        {
            try
            {
                foreach (Message msg in m_tcp.ReadNext())
                {
                    switch (msg.Type)
                    {
                        case MsgType.HELLO:
                            break;
                        case MsgType.KEY:
                            break;
                        case MsgType.NICK:
                            if (msg.GetParam<string>(0) == "OK")
                            {
                                AddLog("Nickname OK");
                                m_tcp.ReceiveTimeout = 0;
                                SetUI(UIState.Connected);
                            }
                            else
                            {
                                AddLog(msg.GetParam<string>(0));
                                DoDisconnect();
                                return; // exit readNext loop
                            }
                            break;
                        case MsgType.MSG:
                            AddMessage(msg.GetParam<string>(0), msg.GetParam<string>(1));
                            break;
                        case MsgType.SYS:
                            AddLog(msg.GetParam<string>(0));
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                AddLog(ex.ToString());
            }
            finally
            {
                if (m_UIState != UIState.Disconnecting)
                {
                    AddLog("Connection Lost");
                }
                m_tcp.Dispose();
                SetUI(UIState.Idle);
                AddLog("Disconeccted");
            }
        }

        public void Sendmsg()
        {
            if (sendBox.Text == "") return;

            try
            {
                m_tcp.Send(new(MsgType.MSG, sendBox.Text));
                AddMessage(m_nick, sendBox.Text, true);
                sendBox.Text = "";
            }
            catch
            {
                //ignore
            }
        }

    }
}