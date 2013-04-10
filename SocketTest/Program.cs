using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TarFramework.T_araNet;

namespace SocketTest
{
    class Program
    {
        static    SocketServer socketServer = new SocketServer();

        static void Main(string[] args)
        {
            var b = new Byte();
            var a = 0xff;
            Console.WriteLine(b = (Byte)a);

            socketServer.BinaryInput = new BinaryInputHandler(BinaryInputHandler);//数据处理代理
            socketServer.Connetions = new ConnectionFilter(ConnectionFilter);//连接代理
            socketServer.MessageInput = new MessageInputHandler(MessageInputHandler); //设置 客户端断开
            socketServer.Start(); //启动服务器
            Console.WriteLine("Black Martix Server is Init!!");

            Console.ReadLine();
        }

        public static void MessageInputHandler(string message, SocketAsyncEventArgs socketAsync, int erorr)
        {
            Console.WriteLine(message);
            socketAsync.UserToken = null;
            socketAsync.AcceptSocket.Close();
        }

        public static void BinaryInputHandler(byte[] data, SocketAsyncEventArgs socketAsync) 
        {
            if (socketAsync.UserToken == null)
            {
                socketAsync.UserToken = new BufferReadStream(409600);
            }

            var buff = socketAsync.UserToken as BufferReadStream;

            buff.Write(data);


            byte[] pdata;
            while (buff.Read(out pdata))
            {
                DataOn(data, socketAsync);
            }

            //string message = Encoding.UTF8.GetString(data);
            //Console.WriteLine(socketAsync.AcceptSocket.RemoteEndPoint.ToString() + ":" + message);
        }

        public static  bool ConnectionFilter(SocketAsyncEventArgs socketAsync)
        {
            Console.WriteLine("UserConnection {0}", socketAsync.AcceptSocket.RemoteEndPoint.ToString());
            socketAsync.UserToken = null;
            return true;
        }

        static void DataOn(byte[] data, SocketAsyncEventArgs e)
        {
            try
            {
                //建立一个读取数据包的类 参数是数据包
                //这个类的功能很强大,可以读取数据包的数据,并可以把你发送过来的对象数据,转换对象引用

                ReadBytes read = new ReadBytes(data);

                int lengt; //数据包长度,用于验证数据包的完整性
                int cmd; //数据包命令类型

                //注意这里一定要这样子写,这样子可以保证所有你要度的数据是完整的,如果读不出来 Raed方法会返回FALSE,从而避免了错误的数据导致崩溃
                if (read.ReadInt32(out lengt) && read.Length == lengt)
                {  //read.Read系列函数是不会产生异常的
                    int id;
                    string mn;
                    SocketClinetTest.PPo  temp;
                    //if (read.ReadInt32(out id) && read.ReadObject<SocketClinetTest.PPo>(out temp))
                    //{

                    //    if (temp != null)
                    //    {
                    //        Console.WriteLine("Id:{0}\r\n Mn:{1} \r\n Guid:{2} \r\n DataLength:{3} \r\n\r\n", temp.Id, temp.Message, temp.guid, read.Length);
                    //    } 
                    //}

                    if (read.ReadObject<SocketClinetTest.PPo>(out temp))
                    {

                        if (temp != null)
                        {
                            Console.WriteLine("Id:{0}\r\n Mn:{1} \r\n Guid:{2} \r\n DataLength:{3} \r\n\r\n", temp.Id, temp.Message, temp.guid, read.Length);
                        }
                    }


                    //根据命令读取数据包
                    //switch (cmd)
                    //{

                    //    case 1000:
                    //        testClass.PPo temp;
                    //        if (read.ReadObject<testClass.PPo>(out temp))
                    //        {

                    //            if (temp != null)
                    //            {
                    //                Console.WriteLine("Id:{0}\r\n Mn:{1} \r\n Guid:{2} \r\n DataLength:{3} \r\n\r\n", temp.Id, temp.Message, temp.guid, read.Length);

                    //            }
                    //        }
                    //        break;
                    //    case 1001:
                    //        {
                    //            int id;
                    //            string mn;
                    //            Guid guid;

                    //            if (read.ReadInt32(out id) && read.ReadString(out mn) && read.ReadObject<Guid>(out guid))
                    //            {

                    //                Console.WriteLine("Id:{0}\r\n Mn:{1} \r\n Guid:{2} \r\n DataLength:{3} \r\n\r\n", id, mn, guid, read.Length);

                    //            }

                    //        }
                    //        break;
                    //    case 1002:
                    //        {
                    //            int id;
                    //            string mn;
                    //            string guid;

                    //            if (read.ReadInt32(out id) && read.ReadString(out mn) && read.ReadString(out guid))
                    //            {

                    //                Console.WriteLine("Id:{0}\r\n Mn:{1} \r\n Guid:{2} \r\n DataLength:{3} \r\n\r\n", id, mn, guid, read.Length);

                    //            }

                    //        }
                    //        break;
                    //    case 1003:
                    //        {
                    //            server.SendData(e.AcceptSocket, data);

                    //        }
                    //        break;


                    //}


                }
            }
            catch (Exception er)
            {
                Console.WriteLine(er.ToString());
            }
        }
    }
}
