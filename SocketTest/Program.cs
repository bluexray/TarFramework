using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TarFramework.T_araNet;
using TarFramework.Common;

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


        public static string UTF8ToGB2312(string str)
        {
            try
            {
                Encoding utf8 = Encoding.GetEncoding(65001);
                Encoding gb2312 = Encoding.GetEncoding("gb2312");//Encoding.Default ,936
                byte[] temp = utf8.GetBytes(str);
                byte[] temp1 = Encoding.Convert(utf8, gb2312, temp);
                string result = gb2312.GetString(temp1);
                return result;
            }
            catch (Exception ex)//(UnsupportedEncodingException ex)
            {
                return null;
            }
        }

        public static bool WordsIScn(string words)
        {
            string TmmP;

            for (int i = 0; i < words.Length; i++)
            {
                TmmP = words.Substring(i, 1);

                byte[] sarr = System.Text.Encoding.GetEncoding("gb2312").GetBytes(TmmP);

                if (sarr.Length == 2)
                {
                    return true;
                }
            }
            return false;
        }



        static void DataOn(byte[] data, SocketAsyncEventArgs e)
        {
            try
            {
                Console.WriteLine(Marshal.SizeOf(typeof(ID3V1)));
                Console.WriteLine(Marshal.SizeOf(typeof(PPo)));
                
                Console.WriteLine("---------------------------------------------------------------------------");

                
                var stream = new MemoryStream(data);
                using (var reader = new BinaryReader(stream))
                {
                    reader.BaseStream.Seek(-1 * Marshal.SizeOf(typeof(PPo)), SeekOrigin.End);
                    var id3Tag = reader.ReadMarshal<PPo>();
                    Console.WriteLine("Id:{0}\r\n Mn:{1} \r\n Guid:{2} \r\n \r\n\r\n", id3Tag.Id,UTF8ToGB2312(id3Tag.Message), id3Tag.Guid);
                    //Console.WriteLine(WordsIScn(id3Tag.Message).ToString());
                    var s =  Encoding.Default.GetBytes(id3Tag.Message);
                    Console.WriteLine(Encoding.UTF8.GetString(s));

                }

                



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
                            Console.WriteLine("Id:{0}\r\n Mn:{1} \r\n Guid:{2} \r\n DataLength:{3} \r\n\r\n", temp.Id, temp.Message, temp.Guid, read.Length);
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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class PPo
    {
        private int id;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }



        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 44)]
        public string message;

        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        private string guid;

        public string Guid
        {
            get { return guid; }
            set { guid = value; }
        }
    }



    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class ID3V1
    {

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        char[] header = "TAG".ToCharArray();
        public string Header
        {
            get { return new string(header); }
            set { header = value.ToCharArray(); }
        }

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 30)]
        private string title;
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 30)]
        private string artist;
        public string Artist
        {
            get { return artist; }
            set { artist = value; }
        }

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 30)]
        private string album;
        public string Album
        {
            get { return album; }
            set { album = value; }
        }

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        private char[] year;
        public char[] Year
        {
            get { return year; }
            set { year = value; }
        }

        [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 28)]
        private string comment;
        public string Comment
        {
            get { return comment; }
            set { comment = value; }
        }

        public byte Reserve { get; set; }
        public byte Track { get; set; }
        public byte Genre { get; set; }
    }
}
