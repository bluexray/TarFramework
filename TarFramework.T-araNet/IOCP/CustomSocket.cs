using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace TarFramework.T_araNet.IOCP
{
    public class CustomSocket : TcpClient
    {
        private DateTime _TimeCreated;

        public DateTime TimeCreated
        {
            get { return _TimeCreated; }
            set { _TimeCreated = value; }
        }

        public CustomSocket(string host, int port)
            : base(host, port)
        {
            _TimeCreated = DateTime.Now;
        }
    }
}
