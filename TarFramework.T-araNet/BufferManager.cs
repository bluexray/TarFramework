/*##Project : Black Matrix--Multi-Thread Asynchronous Socket Server Framework
 *##Author  : bluexray
 *##create Date: 2013/03/19 
*/

using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace TarFramework.T_araNet
{
    /// <summary>
    /// 发送和接收公共缓冲区
    /// </summary>
    public sealed class BufferManager
    {
        private byte[] m_receiveBuffer;  //  maxSessionCount * receivevBufferSize
        private byte[] m_sendBuffer;	 //	 maxSessionCount * sendBufferSize

        private int m_maxSessionCount;		// the maximum number of connections the sample is designed to handle simultaneously 
        private int m_receiveBufferSize;	// buffer size to use for each socket I/O operation
        private int m_sendBufferSize;
        private int m_numBytes;             // the total number of bytes controlled by the buffer pool
        private int m_opsToPreAlloc = 2;        //read, write (don't alloc buffer space for accepts)

        private int m_bufferBlockIndex;
        private Stack<int> m_bufferBlockIndexStack;

        public BufferManager(int maxSessionCount, int receivevBufferSize, int sendBufferSize)
        {
            m_maxSessionCount = maxSessionCount;
            m_receiveBufferSize = receivevBufferSize;
            m_sendBufferSize = sendBufferSize;
            m_numBytes = maxSessionCount * receivevBufferSize * m_opsToPreAlloc;
            m_bufferBlockIndex = 0;
            m_bufferBlockIndexStack = new Stack<int>();
            //m_bufferBlockIndexStack = new Stack<int>(m_numBytes / m_receiveBufferSize);

            Init();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxSessionCount">the maximum number of connections the sample is designed to handle simultaneously </param>
        /// <param name="receivevBufferSize"></param>
        /// <param name="sendBufferSize"></param>
        /// <param name="PreAlloc">read, write (don't alloc buffer space for accepts)</param>
        public BufferManager(int maxSessionCount, int receivevBufferSize, int sendBufferSize, int PreAlloc)
        {
            m_maxSessionCount = maxSessionCount;
            m_receiveBufferSize = receivevBufferSize;
            m_sendBufferSize = sendBufferSize;
            m_opsToPreAlloc = PreAlloc;
            m_numBytes = maxSessionCount * receivevBufferSize * m_opsToPreAlloc;
            m_bufferBlockIndex = 0;
            m_bufferBlockIndexStack = new Stack<int>();

            Init();
        }

        private void Init()
        {
            m_receiveBuffer = new byte[m_receiveBufferSize * m_maxSessionCount * m_opsToPreAlloc];
            m_sendBuffer = new byte[m_sendBufferSize * m_maxSessionCount * m_opsToPreAlloc];
        }

        public int ReceiveBufferSize
        {
            get { return m_receiveBufferSize; }
        }

        public int SendBufferSize
        {
            get { return m_sendBufferSize; }
        }

        public byte[] ReceiveBuffer
        {
            get { return m_receiveBuffer; }
        }

        public byte[] SendBuffer
        {
            get { return m_sendBuffer; }
        }

        public void FreeBufferBlockIndex(int bufferBlockIndex)
        {
            if (bufferBlockIndex == -1)
            {
                return;
            }

            lock (this)
            {
                m_bufferBlockIndexStack.Push(bufferBlockIndex);
            }
        }

        public int GetBufferBlockIndex()
        {
            lock (this)
            {
                int blockIndex = -1;

                if (m_bufferBlockIndexStack.Count > 0)  // 有用过释放的缓冲块
                {
                    blockIndex = m_bufferBlockIndexStack.Pop();
                }
                else
                {
                    if (m_bufferBlockIndex < m_maxSessionCount)  // 有未用缓冲区块
                    {
                        blockIndex = m_bufferBlockIndex++;
                    }
                }

                return blockIndex;
            }
        }

        public int GetReceivevBufferOffset(int bufferBlockIndex)
        {
            if (bufferBlockIndex == -1)  // 没有使用共享块
            {
                return 0;
            }

            return bufferBlockIndex * m_receiveBufferSize;
        }

        public int GetSendBufferOffset(int bufferBlockIndex)
        {
            if (bufferBlockIndex == -1)  // 没有使用共享块
            {
                return 0;
            }

            return bufferBlockIndex * m_sendBufferSize;
        }

        public void Clear()
        {
            lock (this)
            {
                m_bufferBlockIndexStack.Clear();
                m_receiveBuffer = null;
                m_sendBuffer = null;
            }
        }

        #region  iocp Asynchronous socket
        // Assigns a buffer from the buffer pool to the 
        // specified SocketAsyncEventArgs object
        // <returns>true if the buffer was successfully set, else false</returns>
        public bool SetBuffer(SocketAsyncEventArgs args)
        {

            if (m_bufferBlockIndexStack.Count > 0)
            {
                args.SetBuffer(m_receiveBuffer, m_bufferBlockIndexStack.Pop(), m_receiveBufferSize);
            }
            else
            {
                if ((m_numBytes - m_receiveBufferSize) < m_bufferBlockIndex)
                {
                    return false;
                }
                args.SetBuffer(m_receiveBuffer, m_bufferBlockIndex, m_receiveBufferSize);
                m_bufferBlockIndex += m_receiveBufferSize;
            }
            return true;
        }

        // Removes the buffer from a SocketAsyncEventArg object.  
        // This frees the buffer back to the buffer pool
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            m_bufferBlockIndexStack.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
        #endregion
    }
}
