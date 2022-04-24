using Common.Cipher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChatClient
{
    /// <summary>
    /// FingerPrint.xaml 的交互逻辑
    /// </summary>
    public partial class FingerPrint : Window
    {
        public enum TrustResult
        {
            Cancel,
            TrustOnce,
            Trust
        }
        public TrustResult trustResult = TrustResult.Cancel;

        public FingerPrint(Window parent, X509Certificate2 cert)
        {
            Owner = parent;
            InitializeComponent();
            fingerPrintHexLabel.Content = cert.GetCertHashString();
        }

        private void Trust_Click(object sender, RoutedEventArgs e)
        {
            trustResult = TrustResult.Trust;
            Close();
        }

        private void TrustOnce_Click(object sender, RoutedEventArgs e)
        {
            trustResult = TrustResult.TrustOnce;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            trustResult = TrustResult.Cancel;
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}
