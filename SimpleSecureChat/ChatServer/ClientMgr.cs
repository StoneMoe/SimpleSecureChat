using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServer
{
    internal class ClientMgr
    {
        public static Dictionary<string, Socket> clientMap = new Dictionary<string, Socket>();

        public static void Register(string name, Socket s)
        {
            clientMap.Add(name, s);
        }

        public static void Unregister(string name)
        {
            clientMap.Remove(name);
        }

        public static bool ClientNicknameExisted(string username)
        {
            foreach (var s in clientMap)
            {
                if (s.Key == username)
                {
                    return true;
                }
            }
            return false;
        }

        public static void kickAllClient()
        {
            try
            {
                foreach (var item in clientMap)
                {
                    item.Value.Dispose();
                }
            }
            catch { }

        }

        public static string fmtAllClients()
        {
            string temp = "";
            foreach (var s in clientMap)
            {
                temp += string.Format("\"{0}\" ", s.Key);
            }
            return temp;
        }
    }
}
