using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace FirstMate
{
    public class Client : Terminal.Client
    {
        public bool Blocking { get; set; }

        public Client(TcpClient tcpClient, string banner) : base(tcpClient, banner)
        {
            Blocking = false;
        }
    }
}
