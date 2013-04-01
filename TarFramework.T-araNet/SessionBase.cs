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
    /// �Ự����(������, ����ʵ���� AnalyzeDatagram ����)
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
        /// �����Ͳ�������ʱ, �������޲ι��캯��
        /// </summary>
        protected SessionBase() { }

        /// <summary>
        /// �湹�캯����ʼ������
        /// </summary>
        public virtual void Initiate(int maxDatagramsize, int id, Socket socket, BufferManager bufferManager)
        {
            base.ID = id;
            base.LoginTime = DateTime.Now;

            m_bufferManager = bufferManager;
            m_bufferBlockIndex = bufferManager.GetBufferBlockIndex();

            if (m_bufferBlockIndex == -1)  // û�пտ�, �½�
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
                if (this.State != SessionState.Inactive || m_socket == null)  // Inactive ״̬���� Shutdown
                {
                    return;
                }

                this.State = SessionState.Shutdown;
                try
                {
                    m_socket.Shutdown(SocketShutdown.Both);  // Ŀ�ģ������첽�¼�
                }
                catch (Exception) { }
            }
        }

        public void Close()
        {
            lock (this)
            {
                if (this.State != SessionState.Shutdown || m_socket == null)  // Shutdown ״̬���� Close
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

                try  // һ���ͻ������������� �����Ӻ������Ͽ��������ڸô���������ϵͳ����Ϊ�Ǵ���
                {
                    // ��ʼ�������Ըÿͻ��˵�����
                    int bufferOffset = m_bufferManager.GetReceivevBufferOffset(m_bufferBlockIndex);
                    m_socket.BeginReceive(m_receiveBuffer, bufferOffset, m_bufferManager.ReceiveBufferSize, SocketFlags.None, this.EndReceiveDatagram, this);

                }
                catch (Exception err)  // �� Socket �쳣��׼���رոûỰ
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
                        byte[] data = Encoding.ASCII.GetBytes(datagramText);  // ��������ֽ�����
                        m_socket.BeginSend(data, 0, data.Length, SocketFlags.None, this.EndSendDatagram, this);
                    }
                }
                catch (Exception err)  // д socket �쳣��׼���رոûỰ
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

            if (elapsedSecond > maxSessionTimeout)  // ��ʱ����׼���Ͽ�����
            {
                this.DisconnectType = DisconnectType.Timeout;
                this.SetInactive();  // ���Ϊ���رա�׼���Ͽ�
            }
        }

        #endregion

        #region  private methods

        /// <summary>
        /// ����������ɴ�����, iar ΪĿ��ͻ��� Session
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
                catch (Exception err)  // д socket �쳣��׼���رոûỰ
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
                    // Shutdown ʱ������ ReceiveData����ʱҲ�����յ� 0 �����ݰ�
                    int readBytesLength = m_socket.EndReceive(iar);
                    iar.AsyncWaitHandle.Close();

                    if (readBytesLength == 0)
                    {
                        this.DisconnectType = DisconnectType.Normal;
                        this.State = SessionState.Inactive;
                    }
                    else  // �������ݰ�
                    {
                        this.LastSessionTime = DateTime.Now;
                        // �ϲ����ģ�������ͷ��β�ַ���־��ȡ���ģ������������ݴ�����
                        this.ResolveSessionBuffer(readBytesLength);
                        this.ReceiveDatagram();  // ��������
                    }
                }
                catch (Exception err)  // �� socket �쳣���رոûỰ��ϵͳ����Ϊ�Ǵ������ִ������̫�ࣩ
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
        /// �������ջ����������ݵ����ݻ�����������ζ�һ�����ģ�
        /// </summary>
        private void CopyToDatagramBuffer(int start, int length)
        {
            int datagramLength = 0;
            if (m_datagramBuffer != null)
            {
                datagramLength = m_datagramBuffer.Length;
            }

            Array.Resize(ref m_datagramBuffer, datagramLength + length);  // �������ȣ�m_datagramBuffer Ϊ null ������
            Array.Copy(m_receiveBuffer, start, m_datagramBuffer, datagramLength, length);  // ���������ݰ�������
        }

        #endregion

        #region protected methods

        /// <summary>
        /// ��ȡ��ʱ������������أ�����ʵ�ʹ����ض���
        /// </summary>
        protected virtual void ResolveSessionBuffer(int readBytesLength)
        {

            // �ϴ����°��ķǿ�, ��Ȼ����ʼ�ַ�<
            bool hasHeadDelimiter = (m_datagramBuffer != null);

            int headDelimiter = 1;
            int tailDelimiter = 1;

            int bufferOffset = m_bufferManager.GetReceivevBufferOffset(m_bufferBlockIndex);
            int start = bufferOffset;   // m_receiveBuffer �������а���ʼλ��
            int length = 0;  // �Ѿ������Ľ��ջ���������

            int subIndex = bufferOffset;  // �������±�
            while (subIndex < readBytesLength + bufferOffset)
            {
                if (m_receiveBuffer[subIndex] == '<')  // ���ݰ���ʼ�ַ�<��ǰ���������
                {
                    if (hasHeadDelimiter || length > 0)  // ��� < ǰ�������ݣ�����Ϊ�����
                    {
                        this.OnDatagramDelimiterError();
                    }

                    m_datagramBuffer = null;  // ��հ�����������ʼһ���µİ�

                    start = subIndex;         // �°���㣬��<����λ��
                    length = headDelimiter;   // �°��ĳ��ȣ���<��
                    hasHeadDelimiter = true;  // �°��п�ʼ�ַ�
                }
                else if (m_receiveBuffer[subIndex] == '>')  // ���ݰ��Ľ����ַ�>
                {
                    if (hasHeadDelimiter)  // �������������п�ʼ�ַ�<
                    {
                        length += tailDelimiter;  // ���Ȱ��������ַ���>��

                        this.GetDatagramFromBuffer(start, length); // >ǰ���Ϊ��ȷ��ʽ�İ�

                        start = subIndex + tailDelimiter;  // �°���㣨һ��һ�δ�������ѭ����
                        length = 0;  // �°�����
                    }
                    else  // >ǰ��û�п�ʼ�ַ�����ʱ��Ϊ�����ַ�>Ϊһ���ַ����������Ĵ��������
                    {
                        length++;  //  hasHeadDelimiter = false;
                    }
                }
                else  // ���� < Ҳ�� >�� ��һ���ַ������� + 1
                {
                    length++;
                }
                ++subIndex;
            }

            if (length > 0)  // ʣ�µĴ����������������
            {
                int mergedLength = length;
                if (m_datagramBuffer != null)
                {
                    mergedLength += m_datagramBuffer.Length;
                }

                // ʣ�µİ��ĺ����ַ��Ҳ�������ת�浽���Ļ������У����´δ���
                if (hasHeadDelimiter && mergedLength <= m_maxDatagramSize)
                {
                    this.CopyToDatagramBuffer(start, length);
                }
                else  // �������ַ��򳬳�
                {
                    this.OnDatagramOversizeError();
                    m_datagramBuffer = null;  // ����ȫ������
                }
            }
        }

        /// <summary>
        /// Session��д���, ��������: 
        /// 1) �жϰ���Ч���������(ע�⣺������ֹ����); 
        /// 2) �ֽ���еĸ��ֶ�����
        /// 3) У�������������Ч��
        /// 4) ����ȷ����Ϣ���ͻ���(���÷��� SendDatagram())
        /// 5) �洢�����ݵ����ݿ���
        /// 6) �洢��ԭ�ĵ����ݿ���(��ѡ)
        /// 7) �����ֶ�m_name, ��ʾ���ݰ������ߵ�����/���
        /// 8) ������ط���
        /// </summary>
        protected abstract void AnalyzeDatagram(byte[] datagramBytes);

        protected virtual void GetDatagramFromBuffer(int startPos, int len)
        {
            byte[] datagramBytes;
            if (m_datagramBuffer != null)
            {
                datagramBytes = new byte[len + m_datagramBuffer.Length];
                Array.Copy(m_datagramBuffer, 0, datagramBytes, 0, m_datagramBuffer.Length);  // �ȿ��� Session �����ݻ�����������
                Array.Copy(m_receiveBuffer, startPos, datagramBytes, m_datagramBuffer.Length, len);  // �ٿ��� Session �Ľ��ջ�����������
            }
            else
            {
                datagramBytes = new byte[len];
                Array.Copy(m_receiveBuffer, startPos, datagramBytes, 0, len);  // �ٿ��� Session �Ľ��ջ�����������
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
