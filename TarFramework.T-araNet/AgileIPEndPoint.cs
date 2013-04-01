using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TarFramework.T_araNet
{
    public class AgileIPEndPoint
    {
        public AgileIPEndPoint()
        {
        }

        public AgileIPEndPoint(string ip, int thePort)
        {
            this.iPAddress = ip;
            this.port = thePort;
        }

        #region IPAddress
        private string iPAddress = "";
        public string IPAddress
        {
            get
            {
                return this.iPAddress;
            }
            set
            {
                this.iPAddress = value;
            }
        }
        #endregion

        #region Port
        private int port = 0;
        public int Port
        {
            get
            {
                return this.port;
            }
            set
            {
                this.port = value;
            }
        }
        #endregion

        #region IPEndPoint
        public IPEndPoint IPEndPoint
        {
            get
            {
                return new IPEndPoint(System.Net.IPAddress.Parse(this.iPAddress), this.port);
            }
        }
        #endregion
    }
}
