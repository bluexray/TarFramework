/*##Project : Black Matrix--Multi-Thread Asynchronous Socket Server Framework
 *##Author  : bluexray
 *##create Date: 2013/03/19 
*/

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace TarFramework.T_araNet
{
    public class SocketServerBase<TSession> : IDisposable, ISessionEvent
        where TSession : SessionBase, new()
    {
        #region  member fields

        private Socket m_serverSocket;
        private bool m_serverClosed = true;
        private bool m_serverListenPaused = false;

        private int m_acceptListenTimeInterval = 25;         // 侦听论询时间间隔(ms)
        private int m_checkSessionTableTimeInterval = 100;   // 清理Timer的时间间隔(ms)
        private int m_checkDatagramQueueTimeInterval = 100;  // 检查数据包队列时间休息间隔(ms)
        private int m_servertPort = 3130;

        private int m_sessionSequenceNo = 0;  // sessionID 流水号
        private int m_sessionCount;
        private int m_receivedDatagramCount;
        private int m_errorDatagramCount;
        private int m_datagramQueueLength;

        private int m_databaseExceptionCount;
        private int m_serverExceptCount;
        private int m_sessionExceptionCount;

        private int m_maxSessionCount = 1024;
        private int m_receiveBufferSize = 16 * 1024;  // 16 K
        private int m_sendBufferSize = 16 * 1024;

        private int m_maxDatagramSize = 1024 * 1024;  // 1M
        private int m_maxSessionTimeout = 120;   // 2 minutes
        private int m_maxListenQueueLength = 16;
        private int m_maxSameIPCount = 64;

        private Dictionary<int, TSession> m_sessionTable;

        private bool m_disposed = false;

        private ManualResetEvent m_checkAcceptListenResetEvent;
        private ManualResetEvent m_checkSessionTableResetEvent;
        private ManualResetEvent m_checkDatagramQueueResetEvent;

        private Mutex m_ServerMutex;  // 只能有一个服务器
        private BufferManager m_bufferManager;

        #endregion

        #region  public properties

        public bool Closed
        {
            get { return m_serverClosed; }
        }

        public bool ListenPaused
        {
            get { return m_serverListenPaused; }
        }

        public int ServerPort
        {
            get { return m_servertPort; }
            set { m_servertPort = value; }
        }

        public int ServerExceptionCount
        {
            get { return m_serverExceptCount; }
        }

        public int DatabaseExceptionCount
        {
            get { return m_databaseExceptionCount; }
        }

        public int SessionExceptionCount
        {
            get { return m_sessionExceptionCount; }
        }

        public int SessionCount
        {
            get { return m_sessionCount; }
        }

        public int ReceivedDatagramCount
        {
            get { return m_receivedDatagramCount; }
        }

        public int ErrorDatagramCount
        {
            get { return m_errorDatagramCount; }
        }

        public int DatagramQueueLength
        {
            get { return m_datagramQueueLength; }
        }

        [Obsolete("Use AcceptListenTimeInterval instead.")]
        public int LoopWaitTime
        {
            get { return m_acceptListenTimeInterval; }
            set { this.AcceptListenTimeInterval = value; }
        }

        public int AcceptListenTimeInterval
        {
            get { return m_acceptListenTimeInterval; }
            set
            {
                if (value < 0)
                {
                    m_acceptListenTimeInterval = value;
                }
                else
                {
                    m_acceptListenTimeInterval = value;
                }
            }
        }

        public int CheckSessionTableTimeInterval
        {
            get { return m_checkSessionTableTimeInterval; }
            set
            {
                if (value < 10)
                {
                    m_checkSessionTableTimeInterval = 10;
                }
                else
                {
                    m_checkSessionTableTimeInterval = value;
                }
            }
        }

        public int CheckDatagramQueueTimeInterval
        {
            get { return m_checkDatagramQueueTimeInterval; }
            set
            {
                if (value < 10)
                {
                    m_checkDatagramQueueTimeInterval = 10;
                }
                else
                {
                    m_checkDatagramQueueTimeInterval = value;
                }
            }
        }

        public int MaxSessionCount
        {
            get { return m_maxSessionCount; }
        }

        [Obsolete]
        public int MaxSessionTableLength
        {
            get { return m_maxSessionCount; }
            set
            {
                if (value <= 1)
                {
                    m_maxSessionCount = 1;
                }
                else
                {
                    m_maxSessionCount = value;
                }
            }
        }

        public int ReceiveBufferSize
        {
            get { return m_receiveBufferSize; }
        }

        public int SendBufferSize
        {
            get { return m_sendBufferSize; }
        }

        [Obsolete]
        public int MaxReceiveBufferSize
        {
            get { return m_receiveBufferSize; }
            set
            {
                if (value < 1024)
                {
                    m_receiveBufferSize = 1024;
                    m_sendBufferSize = 1024;
                }
                else
                {
                    m_receiveBufferSize = value;
                    m_sendBufferSize = value;
                }
            }
        }

        public int MaxDatagramSize
        {
            get { return m_maxDatagramSize; }
            set {
                m_maxDatagramSize = value < 1024 ? 1024 : value;
            }
        }

        public int MaxListenQueueLength
        {
            get { return m_maxListenQueueLength; }
            set {
                m_maxListenQueueLength = value <= 1 ? 2 : value;
            }
        }

        public int MaxSessionTimeout
        {
            get { return m_maxSessionTimeout; }
            set {
                m_maxSessionTimeout = value < 120 ? 120 : value;
            }
        }

        public int MaxSameIPCount
        {
            get { return m_maxSameIPCount; }
            set {
                m_maxSameIPCount = value < 1 ? 1 : value;
            }
        }

        [Obsolete("Use SessionCoreInfoCollection instead.")]
        public List<SessionCoreInfo> SessionCoreInfoList
        {
            get
            {
                var sessionList = new List<SessionCoreInfo>();
                lock (m_sessionTable)
                {
                    /* •—————————————————————————————————————————————————————•
                       | foreach (TSession session in m_sessionTable.Values) |
                       | {                                                   |
                       |     sessionList.Add((TSessionCoreInfo)session);     |
                       | }                                                   |
                       •—————————————————————————————————————————————————————• */
                    sessionList.AddRange(m_sessionTable.Values.Cast<SessionCoreInfo>());
                }
                return sessionList;
            }
        }

        public Collection<SessionCoreInfo> SessionCoreInfoCollection
        {
            get
            {
                var sessionCollection = new Collection<SessionCoreInfo>();
                lock (m_sessionTable)
                {
                    foreach (var session in m_sessionTable.Values)
                    {
                        sessionCollection.Add((SessionCoreInfo)session);
                    }
                }
                return sessionCollection;
            }
        }

        #endregion

        #region  class events

        public event EventHandler ServerStarted;
        public event EventHandler ServerClosed;
        public event EventHandler ServerListenPaused;
        public event EventHandler ServerListenResumed;
        public event EventHandler<ExceptionEventArgs> ServerException;

        public event EventHandler SessionRejected;
        public event EventHandler<SessionEventArgs> SessionConnected;
        public event EventHandler<SessionEventArgs> SessionDisconnected;
        public event EventHandler<SessionEventArgs> SessionTimeout;

        public event EventHandler<SessionEventArgs> DatagramDelimiterError;
        public event EventHandler<SessionEventArgs> DatagramOversizeError;
        public event EventHandler<SessionExceptionEventArgs> SessionReceiveException;
        public event EventHandler<SessionExceptionEventArgs> SessionSendException;
        public event EventHandler<SessionEventArgs> DatagramAccepted;
        public event EventHandler<SessionEventArgs> DatagramError;
        public event EventHandler<SessionEventArgs> DatagramHandled;

        public event EventHandler<ExceptionEventArgs> DatabaseOpenException;
        public event EventHandler<ExceptionEventArgs> DatabaseCloseException;
        public event EventHandler<ExceptionEventArgs> DatabaseException;

        public event EventHandler<ExceptionEventArgs> ShowDebugMessage;

        #endregion

        #region  class constructor

        public SocketServerBase()
        {
            this.Initiate(m_maxSessionCount, m_receiveBufferSize, m_sendBufferSize);
        }

        public SocketServerBase(string dbConnectionString)
        {
            this.Initiate(m_maxSessionCount, m_receiveBufferSize, m_sendBufferSize);
        }

        public SocketServerBase(int maxSessionCount, int receiveBufferSize, int sendBufferSize)
        {
            this.Initiate(maxSessionCount, receiveBufferSize, sendBufferSize);
        }

        [Obsolete]
        public SocketServerBase(int tcpPort)
        {
            m_servertPort = tcpPort;
            this.Initiate(m_maxSessionCount, m_receiveBufferSize, m_sendBufferSize);
        }

        private void Initiate(int maxSessionCount, int receiveBufferSize, int sendBufferSize)
        {
            bool canCreateNew;
            m_ServerMutex = new Mutex(true, "BlackMatrix _Server", out canCreateNew);
            if (!canCreateNew)
            {
                throw new Exception("Can create two or more server!");
            }

            m_maxSessionCount = maxSessionCount;
            m_receiveBufferSize = receiveBufferSize;
            m_sendBufferSize = sendBufferSize;

            m_bufferManager = new BufferManager(maxSessionCount, receiveBufferSize, sendBufferSize);
            m_sessionTable = new Dictionary<int, TSession>();

            m_checkAcceptListenResetEvent = new ManualResetEvent(true);
            m_checkSessionTableResetEvent = new ManualResetEvent(true);
            m_checkDatagramQueueResetEvent = new ManualResetEvent(true);
        }


        ~SocketServerBase()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            if (!m_disposed)
            {
                m_disposed = true;
                this.Close();
                this.Dispose(true);
                GC.SuppressFinalize(this);  // Finalize 不会第二次执行
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)  // 对象正在被显示释放, 不是执行 Finalize()
            {
                m_sessionTable = null;  // 释放托管资源
            }

            if (m_ServerMutex != null)
            {
                m_ServerMutex.Close();
            }

            if (m_checkAcceptListenResetEvent != null)
            {
                m_checkAcceptListenResetEvent.Close();  // 释放非托管资源
            }

            if (m_checkSessionTableResetEvent != null)
            {
                m_checkSessionTableResetEvent.Close();
            }

            if (m_checkDatagramQueueResetEvent != null)
            {
                m_checkDatagramQueueResetEvent.Close();
            }

            if (m_bufferManager != null)
            {
                m_bufferManager.Clear();
            }
        }

        #endregion

        #region  public methods

        public bool Start()
        {
            if (!m_serverClosed)
            {
                return true;
            }

            m_serverClosed = true;  // 在其它方法中要判断该字段
            m_serverListenPaused = true;

            this.Close();
            this.ClearCountValues();

            try
            {
                if (!this.CreateServerSocket()) return false;
                if (!ThreadPool.QueueUserWorkItem(this.CheckDatagramQueue)) return false;
                if (!ThreadPool.QueueUserWorkItem(this.StartServerListen)) return false;
                if (!ThreadPool.QueueUserWorkItem(this.CheckSessionTable)) return false;

                m_serverClosed = false;
                m_serverListenPaused = false;

                this.OnServerStarted();
            }
            catch (Exception err)
            {
                this.OnServerException(err);
            }
            return !m_serverClosed;
        }

        public void PauseListen()
        {
            m_serverListenPaused = true;
            OnServerListenPaused();
        }

        public void ResumeListen()
        {
            m_serverListenPaused = false;
            this.OnServerListenResumed();
        }

        public void Stop()
        {
            this.Close();
        }

        public void CloseSession(int sessionId)
        {
            TSession session = null;
            lock (m_sessionTable)
            {
                if (m_sessionTable.ContainsKey(sessionId))  // 包含该会话 ID
                {
                    session = (TSession)m_sessionTable[sessionId];
                }
            }

            if (session != null)
            {
                session.SetInactive();
            }
        }

        public void CloseAllSessions()
        {
            lock (m_sessionTable)
            {
                foreach (TSession session in m_sessionTable.Values)
                {
                    session.SetInactive();
                }
            }
        }

        public void SendToSession(int sessionId, string datagramText)
        {
            TSession session = null;
            lock (m_sessionTable)
            {
                session = (TSession)m_sessionTable[sessionId];
            }

            if (session != null)
            {
                session.SendDatagram(datagramText);
            }
        }

        public void SendToAllSessions(string datagramText)
        {
            lock (m_sessionTable)
            {
                foreach (TSession session in m_sessionTable.Values)
                {
                    session.SendDatagram(datagramText);
                }
            }
        }

        #endregion

        #region  private methods

        private void Close()
        {
            if (m_serverClosed)
            {
                return;
            }

            m_serverClosed = true;
            m_serverListenPaused = true;

            m_checkAcceptListenResetEvent.WaitOne();  // 等待3个线程
            m_checkSessionTableResetEvent.WaitOne();
            m_checkDatagramQueueResetEvent.WaitOne();

            if (m_sessionTable != null)
            {
                lock (m_sessionTable)
                {
                    foreach (TSession session in m_sessionTable.Values)
                    {
                        session.Close();
                    }
                }
            }

            this.CloseServerSocket();

            if (m_sessionTable != null)  // 清空会话列表
            {
                lock (m_sessionTable)
                {
                    m_sessionTable.Clear();
                }
            }

            this.OnServerClosed();
        }

        private void ClearCountValues()
        {
            m_sessionCount = 0;
            m_receivedDatagramCount = 0;
            m_errorDatagramCount = 0;
            m_datagramQueueLength = 0;

            m_databaseExceptionCount = 0;
            m_serverExceptCount = 0;
            m_sessionExceptionCount = 0;
        }

        private bool CreateServerSocket()
        {
            try
            {
                m_serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_serverSocket.Bind(new IPEndPoint(IPAddress.Any, m_servertPort));
                m_serverSocket.Listen(m_maxListenQueueLength);

                return true;
            }
            catch (Exception err)
            {
                this.OnServerException(err);
                return false;
            }
        }

        private bool CheckSocketIP(Socket clientSocket)
        {
            IPEndPoint iep = (IPEndPoint)clientSocket.RemoteEndPoint;
            string ip = iep.Address.ToString();

            if (ip.Substring(0, 7) == "127.0.0")   // local machine
            {
                return true;
            }

            lock (m_sessionTable)
            {
                int sameIPCount = 0;
                foreach (TSession session in m_sessionTable.Values)
                {
                    if (session.IP == ip)
                    {
                        sameIPCount++;
                        if (sameIPCount > m_maxSameIPCount)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 侦听客户端连接请求
        /// </summary>
        private void StartServerListen(object state)
        {
            m_checkAcceptListenResetEvent.Reset();
            Socket clientSocket = null;

            while (!m_serverClosed)
            {
                if (m_serverListenPaused)  // pause server
                {
                    this.CloseServerSocket();
                    Thread.Sleep(m_acceptListenTimeInterval);
                    continue;
                }

                if (m_serverSocket == null)
                {
                    this.CreateServerSocket();
                    continue;
                }

                try
                {
                    if (m_serverSocket.Poll(m_acceptListenTimeInterval, SelectMode.SelectRead))
                    {
                        // 频繁关闭、启动时，这里容易产生错误（提示套接字只能有一个）
                        clientSocket = m_serverSocket.Accept();

                        if (clientSocket != null && clientSocket.Connected)
                        {
                            if (m_sessionCount >= m_maxSessionCount || !this.CheckSocketIP(clientSocket))  // 当前列表已经存在该 IP 地址
                            {
                                this.OnSessionRejected(); // 拒绝登录请求
                                this.CloseClientSocket(clientSocket);
                            }
                            else
                            {
                                this.AddSession(clientSocket);  // 添加到队列中, 并调用异步接收方法
                            }
                        }
                        else  // clientSocket is null or connected == false
                        {
                            this.CloseClientSocket(clientSocket);
                        }
                    }
                }
                catch (Exception)  // 侦听连接的异常频繁, 不捕获异常
                {
                    this.CloseClientSocket(clientSocket);
                }
            }

            m_checkAcceptListenResetEvent.Set();
        }

        private void CloseServerSocket()
        {
            if (m_serverSocket == null)
            {
                return;
            }

            try
            {
                lock (m_sessionTable)
                {
                    if (m_sessionTable != null && m_sessionTable.Count > 0)
                    {
                        // 可能结束服务器端的 AcceptClientConnect 的 Poll
                        //                        m_serverSocket.Shutdown(SocketShutdown.Both);  // 有连接才关
                    }
                }
                m_serverSocket.Close();
            }
            catch (Exception err)
            {
                this.OnServerException(err);
            }
            finally
            {
                m_serverSocket = null;
            }
        }

        /// <summary>
        /// 强制关闭客户端请求时的 Socket
        /// </summary>
        private void CloseClientSocket(Socket socket)
        {
            if (socket != null)
            {
                try
                {
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                catch (Exception) { }  // 强制关闭, 忽略错误
            }
        }

        /// <summary>
        /// 增加一个会话对象
        /// </summary>
        private void AddSession(Socket clientSocket)
        {
            Interlocked.Increment(ref m_sessionSequenceNo);

            var session = new TSession();
            session.Initiate(m_maxDatagramSize, m_sessionSequenceNo, clientSocket, m_bufferManager);

            session.DatagramDelimiterError += new EventHandler<SessionEventArgs>(this.OnDatagramDelimiterError);
            session.DatagramOversizeError += new EventHandler<SessionEventArgs>(this.OnDatagramOversizeError);
            session.DatagramError += new EventHandler<SessionEventArgs>(this.OnDatagramError);
            session.DatagramAccepted += new EventHandler<SessionEventArgs>(this.OnDatagramAccepted);
            session.DatagramHandled += new EventHandler<SessionEventArgs>(this.OnDatagramHandled);
            session.SessionReceiveException += new EventHandler<SessionExceptionEventArgs>(this.OnSessionReceiveException);
            session.SessionSendException += new EventHandler<SessionExceptionEventArgs>(this.OnSessionSendException);

            session.ShowDebugMessage += new EventHandler<ExceptionEventArgs>(this.ShowDebugMessage);

            lock (m_sessionTable)
            {
                m_sessionTable.Add(session.ID, session);
            }
            session.ReceiveDatagram();

            this.OnSessionConnected(session);
        }

        /// <summary>
        /// 资源清理线程, 分若干步完成
        /// </summary>
        private void CheckSessionTable(object state)
        {
            m_checkSessionTableResetEvent.Reset();

            while (!m_serverClosed)
            {
                lock (m_sessionTable)
                {
                    var sessionIDList = new List<int>();

                    foreach (TSession session in m_sessionTable.Values)
                    {
                        if (m_serverClosed)
                        {
                            break;
                        }

                        if (session.State == SessionState.Inactive)  // 分三步清除一个 Session
                        {
                            session.Shutdown();  // 第一步: shutdown, 结束异步事件
                        }
                        else if (session.State == SessionState.Shutdown)
                        {
                            session.Close();  // 第二步: Close
                        }
                        else if (session.State == SessionState.Closed)
                        {
                            sessionIDList.Add(session.ID);
                            this.DisconnectSession(session);

                        }
                        else // 正常的会话 Active
                        {
                            session.CheckTimeout(m_maxSessionTimeout); // 判超时，若是则标记
                        }
                    }

                    foreach (int id in sessionIDList)  // 统一清除
                    {
                        m_sessionTable.Remove(id);
                    }

                    sessionIDList.Clear();
                }

                Thread.Sleep(m_checkSessionTableTimeInterval);
            }

            m_checkSessionTableResetEvent.Set();
        }

        /// <summary>
        /// 数据包处理线程
        /// </summary>
        private void CheckDatagramQueue(object state)
        {
            m_checkDatagramQueueResetEvent.Reset();

            while (!m_serverClosed)
            {
                lock (m_sessionTable)
                {
                    foreach (TSession session in m_sessionTable.Values)
                    {
                        if (m_serverClosed)
                        {
                            break;
                        }

                        session.HandleDatagram();
                    }
                }
                Thread.Sleep(m_checkDatagramQueueTimeInterval);
            }

            m_checkDatagramQueueResetEvent.Set();
        }

        private void DisconnectSession(TSession session)
        {
            if (session.DisconnectType == DisconnectType.Normal)
            {
                this.OnSessionDisconnected(session);
            }
            else if (session.DisconnectType == DisconnectType.Timeout)
            {
                this.OnSessionTimeout(session);
            }
        }

        /// <summary>
        /// 输出调试信息
        /// </summary>
        private void OnShowDebugMessage(string message)
        {
            if (this.ShowDebugMessage != null)
            {
                var e = new ExceptionEventArgs(message);
                this.ShowDebugMessage(this, e);
            }
        }

        #endregion

        #region  protected virtual methods

        protected virtual void OnDatabaseOpenException(object sender, ExceptionEventArgs e)
        {
            Interlocked.Increment(ref m_databaseExceptionCount);

            EventHandler<ExceptionEventArgs> handler = this.DatabaseOpenException;
            if (handler != null)
            {
                handler(sender, e);  // 转发事件的激发者
            }
        }

        protected virtual void OnDatabaseCloseException(object sender, ExceptionEventArgs e)
        {
            Interlocked.Increment(ref m_databaseExceptionCount);

            EventHandler<ExceptionEventArgs> handler = this.DatabaseCloseException;
            if (handler != null)
            {
                handler(sender, e);  // 转发事件的激发者
            }
        }

        protected virtual void OnDatabaseException(object sender, ExceptionEventArgs e)
        {
            Interlocked.Increment(ref m_databaseExceptionCount);

            EventHandler<ExceptionEventArgs> handler = this.DatabaseException;
            if (handler != null)
            {
                handler(sender, e);  // 转发事件的激发者
            }
        }

        protected virtual void OnSessionRejected()
        {
            EventHandler handler = this.SessionRejected;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnSessionConnected(TSession session)
        {
            Interlocked.Increment(ref m_sessionCount);

            EventHandler<SessionEventArgs> handler = this.SessionConnected;
            if (handler != null)
            {
                var e = new SessionEventArgs(session);
                handler(this, e);
            }
        }

        protected virtual void OnSessionDisconnected(TSession session)
        {
            Interlocked.Decrement(ref m_sessionCount);

            EventHandler<SessionEventArgs> handler = this.SessionDisconnected;
            if (handler != null)
            {
                var e = new SessionEventArgs(session);
                handler(this, e);
            }
        }

        protected virtual void OnSessionTimeout(TSession session)
        {
            Interlocked.Decrement(ref m_sessionCount);

            EventHandler<SessionEventArgs> handler = this.SessionTimeout;
            if (handler != null)
            {
                var e = new SessionEventArgs(session);
                handler(this, e);
            }
        }

        protected virtual void OnSessionReceiveException(object sender, SessionExceptionEventArgs e)
        {
            Interlocked.Decrement(ref m_sessionCount);
            Interlocked.Increment(ref m_sessionExceptionCount);

            EventHandler<SessionExceptionEventArgs> handler = this.SessionReceiveException;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnSessionSendException(object sender, SessionExceptionEventArgs e)
        {
            Interlocked.Decrement(ref m_sessionCount);
            Interlocked.Increment(ref m_sessionExceptionCount);

            EventHandler<SessionExceptionEventArgs> handler = this.SessionSendException;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnServerException(Exception err)
        {
            Interlocked.Increment(ref m_serverExceptCount);

            EventHandler<ExceptionEventArgs> handler = this.ServerException;
            if (handler != null)
            {
                var e = new ExceptionEventArgs(err);
                handler(this, e);
            }
        }

        protected virtual void OnServerStarted()
        {
            EventHandler handler = this.ServerStarted;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnServerListenPaused()
        {
            var handler = this.ServerListenPaused;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnServerListenResumed()
        {
            EventHandler handler = this.ServerListenResumed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnServerClosed()
        {
            EventHandler handler = this.ServerClosed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        protected virtual void OnDatagramDelimiterError(object sender, SessionEventArgs e)
        {
            Interlocked.Increment(ref m_receivedDatagramCount);
            Interlocked.Increment(ref m_errorDatagramCount);

            EventHandler<SessionEventArgs> handler = this.DatagramDelimiterError;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnDatagramOversizeError(object sender, SessionEventArgs e)
        {
            Interlocked.Increment(ref m_receivedDatagramCount);
            Interlocked.Increment(ref m_errorDatagramCount);

            EventHandler<SessionEventArgs> handler = this.DatagramOversizeError;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnDatagramAccepted(object sender, SessionEventArgs e)
        {
            Interlocked.Increment(ref m_receivedDatagramCount);
            Interlocked.Increment(ref m_datagramQueueLength);

            EventHandler<SessionEventArgs> handler = this.DatagramAccepted;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnDatagramError(object sender, SessionEventArgs e)
        {
            Interlocked.Increment(ref m_errorDatagramCount);
            Interlocked.Decrement(ref m_datagramQueueLength);

            EventHandler<SessionEventArgs> handler = this.DatagramError;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnDatagramHandled(object sender, SessionEventArgs e)
        {
            Interlocked.Decrement(ref m_datagramQueueLength);

            EventHandler<SessionEventArgs> handler = this.DatagramHandled;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        #endregion

    }
}
