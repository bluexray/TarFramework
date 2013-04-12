using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TarFramework.T_araNet.IOCP;

namespace SocketClinetTest
{
    class Program
    {
        public static SocketClient client = new SocketClient();

        static void Main(string[] args)
        {

            client.DataOn += new DataOn(client_DataOn); //数据包进入事件

            client.Disconnection += new ExceptionDisconnection(client_Disconnection); //数据包断开事件

            if (client.ConnectionTo("127.0.0.1", 9989)) //使用同步连接到服务器，一步就用Begin开头的那个
            {
                while (true)
                {
                    Console.ReadLine();



                    //var temp = new PPo();
                    //temp.Id = 2;
                    //temp.Message = "通过对象通讯";
                    //temp.guid =Guid.NewGuid();

                    ////for (int i = 0; i < 100; i++)
                    ////{
                    ////    temp.guid.Add(Guid.NewGuid());
                    ////}
                    //client.SendTo(BufferFormat.FormatFCA(temp));  //讲一个PPO对象发送出去

                    // Console.ReadLine();
                    string str = "通过组合数据包通讯，GUID is object";

                    //BufferFormat buffmat = new BufferFormat(1001);
                    //buffmat.AddItem(3);
                    //buffmat.AddItem(str);
                    //buffmat.AddItem(Guid.NewGuid());
                    //client.SendTo(buffmat.Finish()); 

                    List<byte> buffer = new List<byte>();
                    buffer.AddRange(BitConverter.GetBytes(3));
                    var data = Encoding.UTF8.GetBytes(str);
                    buffer.AddRange(data);
                    buffer.AddRange(Guid.NewGuid().ToByteArray());


                    var p = new byte[buffer.Count];
                    buffer.CopyTo(0, p,0, p.Length);

                    client.SendTo(p);

                    //BufferFormat buffmat = new BufferFormat(1001);
                    //buffmat.AddItem(2);
                    //buffmat.AddItem("通过组合数据包通讯，GUID is object");
                    //buffmat.AddItem(Guid.NewGuid());

                    //client.SendTo(buffmat.Finish()); //用组合数据包模拟PPO对象

                    // Console.ReadLine();

                    //BufferFormat buffmat2 = new BufferFormat(1002);
                    //buffmat2.AddItem(3);
                    //buffmat2.AddItem("通过组合数据包通讯 all buff");
                    //buffmat2.AddItem(Guid.NewGuid().ToString());
                    //client.SendTo(buffmat2.Finish()); //用组合数据包模拟PPO对象 但GUID 是字符串类型

                }

            }
            else
            {
                Console.WriteLine("无法连接服务器");
            }

            Console.ReadLine();
        }

        static void client_Disconnection(string message)
        {
            Console.WriteLine(message);
        }

        static void client_DataOn(byte[] Data)
        {
            throw new NotImplementedException();  
        }
    }

    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class PPo
    {
        private int id;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }



        [MarshalAsAttribute(UnmanagedType.ByValTStr,SizeConst = 44)]
        public string message;

        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string guid;

        public string Guid
        {
            get { return guid; }
            set { guid = value; }
        }
    }
}
