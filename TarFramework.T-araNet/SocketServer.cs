/*##Project : Black Matrix--Multi-Thread Asynchronous Socket Server Framework
 *##Author  : bluexray
 *##create Date: 2013/03/19 
*/
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;


using TarFramework.T_araNet.IOCP;

namespace TarFramework.T_araNet
{
    /// <summary>
    /// 连接的代理
    /// </summary>
    /// <param name="socketAsync"></param>
    public delegate bool ConnectionFilter(SocketAsyncEventArgs socketAsync);

    /// <summary>
    /// 数据包输入代理
    /// </summary>
    /// <param name="data">输入包</param>
    /// <param name="socketAsync"></param>
    public delegate void BinaryInputHandler(byte[] data, SocketAsyncEventArgs socketAsync);

    /// <summary>
    /// 异常错误通常是用户断开的代理
    /// </summary>
    /// <param name="message">消息</param>
    /// <param name="socketAsync"></param>
    /// <param name="erorr">错误代码</param>
    public delegate void MessageInputHandler(string message, SocketAsyncEventArgs socketAsync, int erorr);

    /// <summary>
    /// Black Matrix SOCKET框架 服务器端
    ///（通过6W个连接测试。理论上支持10W个连接，可谓.NET最强SOCKET模型）
    /// </summary>
    public class SocketServer : IDisposable,IServer
    {

        #region 释放
        /// <summary>
        /// 用来确定是否以释放
        /// </summary>
        private bool isDisposed;


        ~SocketServer()
        {
            this.Dispose(false);

        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed||disposing)
            {
                try
                {
                    sock.Shutdown(SocketShutdown.Both);
                    sock.Close();

                    for (int i = 0; i < SocketAsynPool.Count; i++)
                    {
                        SocketAsyncEventArgs args = SocketAsynPool.Pop();

                        BuffManagers.FreeBuffer(args);
                    }

                  
                }
                catch
                {
                }

                isDisposed = true;
            }
        }
        #endregion

        /// <summary>
        /// 数据包管理
        /// </summary>
        private BufferManager BuffManagers;

        /// <summary>
        /// 保证只能创建一个服务端
        /// </summary>
        private Mutex ServerMutex;
        /// <summary>
        /// Socket异步对象池
        /// </summary>
        private SocketAsyncEventArgsPool SocketAsynPool;

        /// <summary>
        /// SOCK对象
        /// </summary>
        private Socket sock;

        /// <summary>
        /// Socket对象
        /// </summary>
        public Socket Sock { get { return sock; } }


        /// <summary>
        /// 连接传入处理
        /// </summary>
        public ConnectionFilter Connetions { get; set; }

        /// <summary>
        /// 数据输入处理
        /// </summary>
        public BinaryInputHandler BinaryInput { get; set; }

        /// <summary>
        /// 异常错误通常是用户断开处理
        /// </summary>
        public MessageInputHandler MessageInput { get; set; }


        private AutoResetEvent[] reset;

        /// <summary>
        /// 是否关闭SOCKET Delay算法
        /// </summary>
        public bool NoDelay
        {
            get
            {
                return sock.NoDelay;
            }

            set
            {
                sock.NoDelay = value;
            }

        }

        /// <summary>
        /// SOCKET 的  ReceiveTimeout属性
        /// </summary>
        public int ReceiveTimeout { get; set; }
        /// <summary>
        /// SOCKET 的 SendTimeout
        /// </summary>
        public int SendTimeout { get; set; }

        public int GetMaxBufferSize { get; private set; }

        /// <summary>
        /// 最大用户连接数
        /// </summary>
        public int GetMaxUserConnect { get; private set; }


        /// <summary>
        /// IP
        /// </summary>
        private string Host;

        /// <summary>
        /// 端口
        /// </summary>
        private int Port;



        #region 消息输出
        /// <summary>
        /// 输出消息
        /// </summary>
        public event EventHandler<LogOutEventArgs> MessageOut;


        /// <summary>
        /// 输出消息
        /// </summary>
        /// <param name="o"></param>
        /// <param name="type"></param>
        /// <param name="message"></param>
        protected void LogOutEvent(Object sender, LogType type, string message)
        {
            if (MessageOut != null)
                MessageOut.BeginInvoke(sender, new LogOutEventArgs(type, message), new AsyncCallback(CallBackEvent), MessageOut);

        }
        /// <summary>
        /// 事件处理完的回调函数
        /// </summary>
        /// <param name="ar"></param>
        private void CallBackEvent(IAsyncResult ar)
        {
            EventHandler<LogOutEventArgs> MessageOut = ar.AsyncState as EventHandler<LogOutEventArgs>;
            if (MessageOut != null)
                MessageOut.EndInvoke(ar);
        }
        #endregion



        #region constructor

        public SocketServer(string host, int port, int maxconnectcout, int maxbuffersize, int receiveTimeout, int sendTime)
        {

            this.Port = port;
            this.Host = host;
            this.GetMaxBufferSize = maxbuffersize;
            this.GetMaxUserConnect = maxconnectcout;
            SendTimeout = sendTime;
            ReceiveTimeout = receiveTimeout;

            Init();
        }

        public SocketServer(string host, int port, int maxconnectcout, int maxbuffersize)
            :this(host="any",port,maxconnectcout,maxbuffersize,1000,1000)
        {
            /* •————————————————————————————————————————————————————————•
               | this.Port = port;                                      |
               | this.Host = host;                                      |
               | this.MaxBufferSize = maxbuffersize;                    |
               | this.GetMaxUserConnect = maxconnectcout;               |
               | SendTimeout = 1000;                                    |
               | ReceiveTimeout = 1000;                                 |
               |                                                        |
               | this.reset = new System.Threading.AutoResetEvent[1];   |
               | reset[0] = new System.Threading.AutoResetEvent(false); |
               |                                                        |
               | Run();                                                 |
               |                                                        |
               •————————————————————————————————————————————————————————• */
        }


        public SocketServer()
            : this("any", 9989, 5000, 1496, 1000, 1000)
        {
            /* •————————————————————————————————————————————————————————•
               | this.Port = 9999;                                      |
               | this.Host = "any";                                     |
               | this.MaxBufferSize = 1496;                             |
               | this.GetMaxUserConnect = 5000;                         |
               | SendTimeout = 1000;                                    |
               | ReceiveTimeout = 1000;                                 |
               |                                                        |
               | this.reset = new System.Threading.AutoResetEvent[1];   |
               | reset[0] = new System.Threading.AutoResetEvent(false); |
               |                                                        |
               | Run();                                                 |
               •————————————————————————————————————————————————————————• */
        }




        #endregion
        /// <summary>
        /// 启动
        /// </summary>
        private void Run()
        {
            if (isDisposed == true)
            {
                throw new ObjectDisposedException("Black Martix Server is Disposed");
            }


            var myEnd = new IPEndPoint(IPAddress.Any, Port);

            if (!Host.Equals("any",StringComparison.CurrentCultureIgnoreCase))
            {
                if (String.IsNullOrEmpty(Host))
                {
                    var p = Dns.GetHostEntry(Dns.GetHostName());

                    foreach (var s in p.AddressList)
                    {
                        if (!s.IsIPv6LinkLocal && s.AddressFamily != AddressFamily.InterNetworkV6)
                        {
                            myEnd = new IPEndPoint(s, Port);
                            break;
                        }
                    }
                  
                }
                else
                {
                    try
                    {
                        myEnd = new IPEndPoint(IPAddress.Parse(Host), Port);
                    }
                    catch (FormatException)
                    {
                        var p = Dns.GetHostEntry(Dns.GetHostName());

                        foreach (var s in p.AddressList)
                        {
                            if (!s.IsIPv6LinkLocal)
                                myEnd = new IPEndPoint(s, Port);
                        }
                    }

                }
            
            
            }

            sock = new Socket(myEnd.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);

    
            sock.Bind(myEnd);
            sock.Listen(50);


            sock.SendTimeout = SendTimeout;
            sock.ReceiveTimeout = ReceiveTimeout;

            BuffManagers = new BufferManager(GetMaxUserConnect , GetMaxBufferSize, GetMaxBufferSize);
            //BuffManagers.Init();

            SocketAsynPool = new SocketAsyncEventArgsPool(GetMaxUserConnect);

            for (int i = 0; i < GetMaxUserConnect; i++)
            {
                SocketAsyncEventArgs socketasyn = new SocketAsyncEventArgs();
                socketasyn.Completed += new EventHandler<SocketAsyncEventArgs>(Asyn_Completed);
                SocketAsynPool.Push(socketasyn);
            }

            Accept();
        }

        public void Start()
        {
            reset[0].Set();
           
        }

        public void Stop()
        {
            reset[0].Reset();
        }

        void Accept()
        {
           
            
            if (SocketAsynPool.Count > 0)
            {
                SocketAsyncEventArgs sockasyn = SocketAsynPool.Pop();
                if (!Sock.AcceptAsync(sockasyn))
                {
                    BeginAccep(sockasyn);
                }
            }
            else
            {
                LogOutEvent(null, LogType.Error, "The MaxUserCout");
            }
        }

        void BeginAccep(SocketAsyncEventArgs e)
        {
            try
            {
                

                if (e.SocketError == SocketError.Success)
                {

                    System.Threading.WaitHandle.WaitAll(reset);
                    reset[0].Set();

                    if (this.Connetions != null)
                        if (!this.Connetions(e))
                        {

                            LogOutEvent(null, LogType.Error, string.Format("The Socket Not Connect {0}", e.AcceptSocket.RemoteEndPoint));
                            e.AcceptSocket = null;
                            SocketAsynPool.Push(e);

                            return;
                        }

                   

                    if (BuffManagers.SetBuffer(e))
                    {
                        if (!e.AcceptSocket.ReceiveAsync(e))
                        {
                            BeginReceive(e);
                        }
                       
                    }

                }
                else
                {
                    e.AcceptSocket = null;
                    SocketAsynPool.Push(e);                 
                    LogOutEvent(null, LogType.Error, "Not Accep");
                }
            }
            finally
            {
                Accept();
            }

        }


      

        void BeginReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success&&e.BytesTransferred>0)
            {
                byte[] data = new byte[e.BytesTransferred];

                System.Buffer.BlockCopy(e.Buffer, e.Offset, data, 0, data.Length);               

                if (this.BinaryInput != null)
                    this.BinaryInput(data, e);
                              
                if (!e.AcceptSocket.ReceiveAsync(e))
                {
                    BeginReceive(e);
                }
            }
            else
            {
                string message=string.Format("User Disconnect :{0}", e.AcceptSocket.RemoteEndPoint.ToString());

                LogOutEvent(null, LogType.Error, message);

                if (MessageInput != null)
                {
                    MessageInput(message, e, 0);
                }

                e.AcceptSocket = null;
                BuffManagers.FreeBuffer(e);
                SocketAsynPool.Push(e);
                if (SocketAsynPool.Count == 1)
                {
                    Accept();
                }
            }

        }

     

        void Asyn_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    BeginAccep(e);
                    break;
                case SocketAsyncOperation.Receive:
                    BeginReceive(e);
                    break;
               
            }
            
        }

        /// <summary>
        /// 发送数据包
        /// </summary>
        /// <param name="sock"></param>
        /// <param name="data"></param>
        public virtual void SendData(Socket sock, byte[] data)
        {
            try
            {
                if (sock != null && sock.Connected)
                    sock.BeginSend(data, 0, data.Length, SocketFlags.None, AsynCallBack, sock);
            }
            catch(SocketException)
            {

            }
        }

        void AsynCallBack(IAsyncResult result)
        {
            try
            {
                Socket sock = result.AsyncState as Socket;

                if (sock != null)
                {
                    sock.EndSend(result);
                }
            }
            catch
            {

            }
        }

        /// <summary>
        /// 断开此SOCKET
        /// </summary>
        /// <param name="sock"></param>
        public void Disconnect(Socket socks)
        {
            try
            {
                if (sock != null)
                    socks.BeginDisconnect(false, AsynCallBackDisconnect, socks);
            }
            catch (ObjectDisposedException)
            {
            }

        }

        void AsynCallBackDisconnect(IAsyncResult result)
        {
            try
            {
                Socket sock = result.AsyncState as Socket;

                if (sock != null)
                {
                    sock.Shutdown(SocketShutdown.Both);
                    sock.EndDisconnect(result);
                }
            }
            catch (ObjectDisposedException)
            {
            }
        }


        public void Init()
        {
            bool canCreateNew = false;
            ServerMutex = new Mutex(true, "Black_Martix_SERVER", out canCreateNew);
            if (!canCreateNew)
            {
                throw new Exception("Can create two or more server!");
            }

            this.reset = new System.Threading.AutoResetEvent[1];
            reset[0] = new System.Threading.AutoResetEvent(false);

            Run();
        }
    }
}