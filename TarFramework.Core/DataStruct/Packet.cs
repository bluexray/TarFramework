using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarFramework.Core.DataStruct
{
    public class Packet : IDisposable
    {
        public byte Header { get; set; } //包头
        public byte Version { get; set; } //探针版本
        public byte[] PacketData = new byte[4048];//包内容
        public ProtocolType protocoltype;//包协议
        public CommondInfo commond;//包命令
        public byte[] key = new byte[8];//包密钥
        public byte EOF { get; set; }//包结束符

        public void Dispose()
        {
            var b = new byte();
            throw new NotImplementedException();
        }
    }
}
