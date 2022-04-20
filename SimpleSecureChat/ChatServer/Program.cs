using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ChatServer
{
    internal class Program
    {

        static void Main(string[] args)
        {
            Server s = new Server();
            s.Loop();
        }

    }
}
