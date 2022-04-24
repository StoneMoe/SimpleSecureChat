using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ChatClient.Visual
{
    internal class Colors
    {
        public static readonly SolidColorBrush Green = new(Color.FromRgb(51, 153, 102));
        public static readonly SolidColorBrush DeepBlue = new(Color.FromRgb(0, 102, 225));
        public static readonly SolidColorBrush SoftRed = new(Color.FromRgb(255, 100, 100));
        public static readonly SolidColorBrush CyanBlue = new(Color.FromArgb(50, 0, 204, 153));
        public static readonly SolidColorBrush Gray = new(Color.FromRgb(128, 128, 128));
    }
}
