using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient.Model
{
    internal class Server
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string SHA1 { get; set; }

    }
}
