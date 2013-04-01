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
            string message = Encoding.UTF8.GetString(data);
            Console.WriteLine(socketAsync.AcceptSocket.RemoteEndPoint.ToString() + ":" + message);
        }

        public static  bool ConnectionFilter(SocketAsyncEventArgs socketAsync)
        {
            Console.WriteLine("UserConnection {0}", socketAsync.AcceptSocket.RemoteEndPoint.ToString());
            socketAsync.UserToken = null;
            return true;
        }
    }
}
