/*##Project : Black Matrix--Multi-Thread Asynchronous Socket Server Framework
 *##Author  : bluexray
 *##create Date: 2013/03/19 
*/
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;


namespace TarFramework.T_araNet
{
    /// <summary>
    /// 会话基类(抽象类, 必须实现其 AnalyzeDatagram 方法)
    /// </summary>
    public abstract class SessionBase : SessionCoreInfo, ISessionEvent
    {
        #region  member fields

        private Socket m_socket;
        private int m_maxDatagramSize;

        private BufferManager m_bufferManager;

        private int m_bufferBlockIndex;
        private byte[] m_receiveBuffer;
        private byte[] m_sendBuffer;

        private byte[] m_datagramBuffer;

        private Queue<byte[]> m_datagramQueue;

        #endregion

        #region class events

        public event EventHandler<SessionExceptionEventArgs> SessionReceiveException;
        public event EventHandler<SessionExceptionEventArgs> SessionSendException;
        public event EventHandler<SessionEventArgs> DatagramDelimiterError;
        public event EventHandler<SessionEventArgs> DatagramOversizeError;
        public event EventHandler<SessionEventArgs> DatagramAccepted;
        public event EventHandler<SessionEventArgs> DatagramError;
        public event EventHandler<SessionEventArgs> DatagramHandled;

        public event EventHandler<ExceptionEventArgs> ShowDebugMessage;

        #endregion

        #region  class constructor
        /// <summary>
        /// 作泛型参数类型时, 必须有无参构造函数
        /// </summary>
        protected SessionBase() { }

        /// <summary>
        /// 替构造函数初始化对象
        /// </summary>
        public virtual void Initiate(int maxDatagramsize, int id, Socket socket, BufferManager bufferManager)
        {
            base.ID = id;
            base.LoginTime = DateTime.Now;

            m_bufferManager = bufferManager;
            m_bufferBlockIndex = bufferManager.GetBufferBlockIndex();

            if (m_bufferBlockIndex == -1)  // 没有空块, 新建
            {
                m_receiveBuffer = new byte[m_bufferManager.ReceiveBufferSize];
                m_sendBuffer = new byte[m_bufferManager.SendBufferSize];
            }
            else
            {
                m_receiveBuffer = m_bufferManager.ReceiveBuffer;
                m_sendBuffer = m_bufferManager.SendBuffer;
            }

            m_maxDatagramSize = maxDatagramsize;

            m_socket = socket;

            m_datagramQueue = new Queue<byte[]>();

            if (m_socket != null)
            {
                IPEndPoint iep = m_socket.RemoteEndPoint as IPEndPoint;
                if (iep != null)
                {
                    base.IP = iep.Address.ToString();
                }
            }
        }

        #endregion

        #region  public methods

        public void Shutdown()
        {
            lock (this)
            {
                if (this.State != SessionState.Inactive || m_socket == null)  // Inactive 状态才能 Shutdown
                {
                    return;
                }

                this.State = SessionState.Shutdown;
                try
                {
                    m_socket.Shutdown(SocketShutdown.Both);  // 目的：结束异步事件
                }
                catch (Exception) { }
            }
        }

        public void Close()
        {
            lock (this)
            {
                if (this.State != SessionState.Shutdown || m_socket == null)  // Shutdown 状态才能 Close
                {
                    return;
                }

                m_datagramBuffer = null;

                if (m_datagramQueue != null)
                {
                    while (m_datagramQueue.Count > 0)
                    {

                        m_datagramQueue.Dequeue();
                    }
                    m_datagramQueue.Clear();
                }

                m_bufferManager.FreeBufferBlockIndex(m_bufferBlockIndex);

                try
                {
                    this.State = SessionState.Closed;
                    m_socket.Close();
                }
                catch (Exception) { }
            }
        }

        public void SetInactive()
        {
            lock (this)
            {
                if (this.State == SessionState.Active)
                {
                    this.State = SessionState.Inactive;
                    this.DisconnectType = DisconnectType.Normal;
                }
            }
        }

        public void HandleDatagram()
        {
            lock (this)
            {
                if (this.State != SessionState.Active || m_datagramQueue.Count == 0)
                {
                    return;
                }

                byte[] datagramBytes = m_datagramQueue.Dequeue();
                this.AnalyzeDatagram(datagramBytes);
            }
        }

        public void ReceiveDatagram()
        {
            lock (this)
            {
                if (this.State != SessionState.Active)
                {
                    return;
                }

                try  // 一个客户端连续做连接 或连接后立即断开，容易在该处产生错误，系统不认为是错误
                {
                    // 开始接受来自该客户端的数据
                    int bufferOffset = m_bufferManager.GetReceivevBufferOffset(m_bufferBlockIndex);
                    m_socket.BeginReceive(m_receiveBuffer, bufferOffset, m_bufferManager.ReceiveBufferSize, SocketFlags.None, this.EndReceiveDatagram, this);

                }
                catch (Exception err)  // 读 Socket 异常，准备关闭该会话
                {
                    this.DisconnectType = DisconnectType.Exception;
                    this.State = SessionState.Inactive;

                    this.OnSessionReceiveException(err);
                }
            }
        }

        public void SendDatagram(string datagramText)
        {
            lock (this)
            {
                if (this.State != SessionState.Active)
                {
                    return;
                }

                try
                {

                    int byteLength = Encoding.ASCII.GetByteCount(datagramText);
                    if (byteLength <= m_bufferManager.SendBufferSize)
                    {
                        int bufferOffset = m_bufferManager.GetSendBufferOffset(m_bufferBlockIndex);
                        Encoding.ASCII.GetBytes(datagramText, 0, byteLength, m_sendBuffer, bufferOffset);
                        m_socket.BeginSend(m_sendBuffer, bufferOffset, byteLength, SocketFlags.None, this.EndSendDatagram, this);
                    }
                    else
                    {
                        byte[] data = Encoding.ASCII.GetBytes(datagramText);  // 获得数据字节数组
                        m_socket.BeginSend(data, 0, data.Length, SocketFlags.None, this.EndSendDatagram, this);
                    }
                }
                catch (Exception err)  // 写 socket 异常，准备关闭该会话
                {
                    this.DisconnectType = DisconnectType.Exception;
                    this.State = SessionState.Inactive;

                    this.OnSessionSendException(err);
                }
            }
        }

        public void CheckTimeout(int maxSessionTimeout)
        {
            TimeSpan ts = DateTime.Now.Subtract(this.LastSessionTime);
            int elapsedSecond = Math.Abs((int)ts.TotalSeconds);

            if (elapsedSecond > maxSessionTimeout)  // 超时，则准备断开连接
            {
                this.DisconnectType = DisconnectType.Timeout;
                this.SetInactive();  // 标记为将关闭、准备断开
            }
        }

        #endregion

        #region  private methods

        /// <summary>
        /// 发送数据完成处理函数, iar 为目标客户端 Session
        /// </summary>
        private void EndSendDatagram(IAsyncResult iar)
        {
            lock (this)
            {
                if (this.State != SessionState.Active)
                {
                    return;
                }

                if (!m_socket.Connected)
                {
                    this.SetInactive();
                    return;
                }

                try
                {
                    m_socket.EndSend(iar);
                    iar.AsyncWaitHandle.Close();
                }
                catch (Exception err)  // 写 socket 异常，准备关闭该会话
                {
                    this.DisconnectType = DisconnectType.Exception;
                    this.State = SessionState.Inactive;

                    this.OnSessionSendException(err);
                }
            }
        }

        private void EndReceiveDatagram(IAsyncResult iar)
        {
            lock (this)
            {
                if (this.State != SessionState.Active)
                {
                    return;
                }

                if (!m_socket.Connected)
                {
                    this.SetInactive();
                    return;
                }

                try
                {
                    // Shutdown 时将调用 ReceiveData，此时也可能收到 0 长数据包
                    int readBytesLength = m_socket.EndReceive(iar);
                    iar.AsyncWaitHandle.Close();

                    if (readBytesLength == 0)
                    {
                        this.DisconnectType = DisconnectType.Normal;
                        this.State = SessionState.Inactive;
                    }
                    else  // 正常数据包
                    {
                        this.LastSessionTime = DateTime.Now;
                        // 合并报文，按报文头、尾字符标志抽取报文，将包交给数据处理器
                        this.ResolveSessionBuffer(readBytesLength);
                        this.ReceiveDatagram();  // 继续接收
                    }
                }
                catch (Exception err)  // 读 socket 异常，关闭该会话，系统不认为是错误（这种错误可能太多）
                {
                    if (this.State == SessionState.Active)
                    {
                        this.DisconnectType = DisconnectType.Exception;
                        this.State = SessionState.Inactive;

                        this.OnSessionReceiveException(err);
                    }
                }
            }
        }

        /// <summary>
        /// 拷贝接收缓冲区的数据到数据缓冲区（即多次读一个包文）
        /// </summary>
        private void CopyToDatagramBuffer(int start, int length)
        {
            int datagramLength = 0;
            if (m_datagramBuffer != null)
            {
                datagramLength = m_datagramBuffer.Length;
            }

            Array.Resize(ref m_datagramBuffer, datagramLength + length);  // 调整长度（m_datagramBuffer 为 null 不出错）
            Array.Copy(m_receiveBuffer, start, m_datagramBuffer, datagramLength, length);  // 拷贝到数据包缓冲区
        }

        #endregion

        #region protected methods

        /// <summary>
        /// 提取包时与包规则紧密相关，根据实际规则重定义
        /// </summary>
        protected virtual void ResolveSessionBuffer(int readBytesLength)
        {

            // 上次留下包文非空, 必然含开始字符<
            bool hasHeadDelimiter = (m_datagramBuffer != null);

            int headDelimiter = 1;
            int tailDelimiter = 1;

            int bufferOffset = m_bufferManager.GetReceivevBufferOffset(m_bufferBlockIndex);
            int start = bufferOffset;   // m_receiveBuffer 缓冲区中包开始位置
            int length = 0;  // 已经搜索的接收缓冲区长度

            int subIndex = bufferOffset;  // 缓冲区下标
            while (subIndex < readBytesLength + bufferOffset)
            {
                if (m_receiveBuffer[subIndex] == '<')  // 数据包开始字符<，前面包文作废
                {
                    if (hasHeadDelimiter || length > 0)  // 如果 < 前面有数据，则认为错误包
                    {
                        this.OnDatagramDelimiterError();
                    }

                    m_datagramBuffer = null;  // 清空包缓冲区，开始一个新的包

                    start = subIndex;         // 新包起点，即<所在位置
                    length = headDelimiter;   // 新包的长度（即<）
                    hasHeadDelimiter = true;  // 新包有开始字符
                }
                else if (m_receiveBuffer[subIndex] == '>')  // 数据包的结束字符>
                {
                    if (hasHeadDelimiter)  // 两个缓冲区中有开始字符<
                    {
                        length += tailDelimiter;  // 长度包括结束字符“>”

                        this.GetDatagramFromBuffer(start, length); // >前面的为正确格式的包

                        start = subIndex + tailDelimiter;  // 新包起点（一般一次处理将结束循环）
                        length = 0;  // 新包长度
                    }
                    else  // >前面没有开始字符，此时认为结束字符>为一般字符，待后续的错误包处理
                    {
                        length++;  //  hasHeadDelimiter = false;
                    }
                }
                else  // 即非 < 也非 >， 是一般字符，长度 + 1
                {
                    length++;
                }
                ++subIndex;
            }

            if (length > 0)  // 剩下的待处理串，分两种情况
            {
                int mergedLength = length;
                if (m_datagramBuffer != null)
                {
                    mergedLength += m_datagramBuffer.Length;
                }

                // 剩下的包文含首字符且不超长，转存到包文缓冲区中，待下次处理
                if (hasHeadDelimiter && mergedLength <= m_maxDatagramSize)
                {
                    this.CopyToDatagramBuffer(start, length);
                }
                else  // 不含首字符或超长
                {
                    this.OnDatagramOversizeError();
                    m_datagramBuffer = null;  // 丢弃全部数据
                }
            }
        }

        /// <summary>
        /// Session重写入口, 基本功能: 
        /// 1) 判断包有效性与包类型(注意：包带起止符号); 
        /// 2) 分解包中的各字段数据
        /// 3) 校验包及其数据有效性
        /// 4) 发送确认消息给客户端(调用方法 SendDatagram())
        /// 5) 存储包数据到数据库中
        /// 6) 存储包原文到数据库中(可选)
        /// 7) 补充字段m_name, 表示数据包发送者的名称/编号
        /// 8) 其它相关方法
        /// </summary>
        protected abstract void AnalyzeDatagram(byte[] datagramBytes);

        protected virtual void GetDatagramFromBuffer(int startPos, int len)
        {
            byte[] datagramBytes;
            if (m_datagramBuffer != null)
            {
                datagramBytes = new byte[len + m_datagramBuffer.Length];
                Array.Copy(m_datagramBuffer, 0, datagramBytes, 0, m_datagramBuffer.Length);  // 先拷贝 Session 的数据缓冲区的数据
                Array.Copy(m_receiveBuffer, startPos, datagramBytes, m_datagramBuffer.Length, len);  // 再拷贝 Session 的接收缓冲区的数据
            }
            else
            {
                datagramBytes = new byte[len];
                Array.Copy(m_receiveBuffer, startPos, datagramBytes, 0, len);  // 再拷贝 Session 的接收缓冲区的数据
            }

            if (m_datagramBuffer != null)
            {
                m_datagramBuffer = null;
            }

            m_datagramQueue.Enqueue(datagramBytes);
        }

        protected virtual void OnDatagramDelimiterError()
        {
            EventHandler<SessionEventArgs> handler = this.DatagramDelimiterError;
            if (handler != null)
            {
                SessionEventArgs e = new SessionEventArgs(this);
                handler(this, e);
            }
        }

        protected virtual void OnDatagramOversizeError()
        {
            EventHandler<SessionEventArgs> handler = this.DatagramOversizeError;
            if (handler != null)
            {
                SessionEventArgs e = new SessionEventArgs(this);
                handler(this, e);
            }
        }

        protected virtual void OnDatagramAccepted()
        {
            EventHandler<SessionEventArgs> handler = this.DatagramAccepted;
            if (handler != null)
            {
                SessionEventArgs e = new SessionEventArgs(this);
                handler(this, e);
            }
        }

        protected virtual void OnDatagramError()
        {
            EventHandler<SessionEventArgs> handler = this.DatagramError;
            if (handler != null)
            {
                SessionEventArgs e = new SessionEventArgs(this);
                handler(this, e);
            }
        }

        protected virtual void OnDatagramHandled()
        {
            EventHandler<SessionEventArgs> handler = this.DatagramHandled;
            if (handler != null)
            {
                SessionEventArgs e = new SessionEventArgs(this);
                handler(this, e);
            }
        }

        protected virtual void OnSessionReceiveException(Exception err)
        {
            EventHandler<SessionExceptionEventArgs> handler = this.SessionReceiveException;
            if (handler != null)
            {
                SessionExceptionEventArgs e = new SessionExceptionEventArgs(err, this);
                handler(this, e);
            }
        }

        protected virtual void OnSessionSendException(Exception err)
        {
            EventHandler<SessionExceptionEventArgs> handler = this.SessionSendException;
            if (handler != null)
            {
                SessionExceptionEventArgs e = new SessionExceptionEventArgs(err, this);
                handler(this, e);
            }
        }

        protected void OnShowDebugMessage(string message)
        {
            if (this.ShowDebugMessage != null)
            {
                ExceptionEventArgs e = new ExceptionEventArgs(message);
                this.ShowDebugMessage(this, e);
            }
        }

        #endregion
    }
}
