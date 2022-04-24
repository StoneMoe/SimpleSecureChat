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
                throw new ArgumentException("Host string is empty", "host");

            IPAddress ip;
            if (!IPAddress.TryParse(host, out ip))
            {
                IPHostEntry entry = Dns.GetHostEntry(host);
                if (entry.AddressList.Length > 0)
                    ip = entry.AddressList[0];
            }

            if (ip == null)
                throw new ArgumentException("Host not found", "host");

            return ip;
        }

        // convert port string to int and check
        public static int ResolvePort(string port)
        {
            if (port == null || port.Length == 0)
                throw new ArgumentException("Port string is empty", "port");

            int port_num;
            if (!int.TryParse(port, out port_num))
                throw new ArgumentException("Port is not a number", "port");

            if (port_num < IPEndPoint.MinPort || port_num > IPEndPoint.MaxPort)
                throw new ArgumentException("Port is out of range", "port");

            return port_num;
        }
    }
}
