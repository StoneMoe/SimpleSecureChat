﻿using Common.Network;
using Common.Network.Data;
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
        public static Dictionary<string, NetClient> clientMap = new();

        public static void Register(string name, NetClient s)
        {
            clientMap.Add(name, s);
        }

        public static void Unregister(NetClient client)
        {
            clientMap.Remove((string)client.state.GetValueOrDefault("nick", ""));
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

        public static void KickAllClient()
        {
            foreach (var item in clientMap)
            {
                try
                {
                    item.Value.Disconnect();
                }
                catch
                {
                    //ignore
                }
            }

        }

        public static string FmtAllClients()
        {
            string temp = "";
            foreach (var s in clientMap)
            {
                temp += string.Format("\"{0}\" ", s.Key);
            }
            return temp;
        }

        public static void Broadcast(Message msg)
        {
            Broadcast(msg, null);
        }
        public static void Broadcast(Message msg, NetClient? except)
        {
            foreach (var s in clientMap)
            {
                try
                {
                    if (except == null || except != s.Value) s.Value.Send(msg);
                }
                catch
                {
                    //ignore
                }
            }
        }
    }
}
