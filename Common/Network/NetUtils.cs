using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Common.Network
{
    internal class NetUtils
    {
        // resolve domain string or ip string to IPAddress
        public static IPAddress Resolve(string host)
        {
            if (host == null || host.Length == 0)
                throw new ArgumentException("Host string is empty", nameof(host));

            if (!IPAddress.TryParse(host, out IPAddress ip))
            {
                IPHostEntry entry = Dns.GetHostEntry(host);
                if (entry.AddressList.Length > 0)
                    ip = entry.AddressList[0];
            }

            if (ip == null)
                throw new ArgumentException("Host not found", nameof(host));

            return ip;
        }

        // convert port string to int and check
        public static int ResolvePort(string port)
        {
            if (port == null || port.Length == 0)
                throw new ArgumentException("Port string is empty", nameof(port));

            if (!int.TryParse(port, out int port_num))
                throw new ArgumentException("Port is not a number", nameof(port));

            if (port_num < IPEndPoint.MinPort || port_num > IPEndPoint.MaxPort)
                throw new ArgumentException("Port is out of range", nameof(port));

            return port_num;
        }
    }
}
