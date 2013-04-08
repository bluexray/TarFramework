using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace TarFramework.T_araNet
{

    /// <summary>
    /// 数据包在读取前需要额外的处理回调方法。（例如解密，解压缩等）
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public delegate byte[] RDataExtraHandle(byte[] data);


    /// <summary>
    /// 数据包读取类
    /// （此类的功能是讲通讯数据包重新转换成.NET 数据类型）
    /// </summary>
    public class ReadBytes
    {

        protected int current;

        public byte[] Data { get; set; }

        protected int startIndex;
        protected int endlengt;

        /// <summary>
        /// 额外处理是否调用成功，可以判断是否解密成功
        /// </summary>
        public bool IsDataExtraSuccess { get; set; }

        /// <summary>
        /// 数据包长度
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 当前其位置
        /// </summary>
        public int Postion
        {
            get
            {
                return current;
            }

            set
            {
                Interlocked.Exchange(ref current, value);
            }
        }

        public virtual void Reset()
        {
            current = 0;
        }


        public ReadBytes()
        {
        }

        public ReadBytes(Byte[] data)
        {
            Data = data;
            this.Length = Data.Length;
            current = 0;
            IsDataExtraSuccess = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startIndex">需要载入数据额外处理的开始位置</param>
        /// <param name="length">需要载入数据额外处理的数据长度 -1为，开始INDEX到结束位置,-2就是保留最后1位</param>
        ///  <param name="dataExtraCallBack"> 数据包在读取前需要额外的处理回调方法。（例如加密，解压缩等）</param>
        public ReadBytes(Byte[] data, int startIndex, int length, RDataExtraHandle dataExtraCallBack)
        {
            try
            {
                this.startIndex = startIndex;
                this.Length = data.Length;
                if (length < 0)
                {
                    endlengt = (data.Length + length + 1) - startIndex;

                }
                else
                {
                    endlengt = length;
                }

                byte[] handBytes = new byte[this.startIndex];

                System.Buffer.BlockCopy(data, 0, handBytes, 0, handBytes.Length); //首先保存不需要解密的数组

                byte[] endBytes = new byte[data.Length - (startIndex + endlengt)];

                System.Buffer.BlockCopy(data, (startIndex + endlengt), endBytes, 0, endBytes.Length); //首先保存不需要解密的数组

                byte[] NeedExByte = new byte[endlengt];

                System.Buffer.BlockCopy(data, startIndex, NeedExByte, 0, NeedExByte.Length);

                if (dataExtraCallBack != null)
                    NeedExByte = dataExtraCallBack(NeedExByte);

                Data = new byte[handBytes.Length + NeedExByte.Length + endBytes.Length]; //重新整合解密完毕后的数据包

                System.Buffer.BlockCopy(handBytes, 0, Data, 0, handBytes.Length);
                System.Buffer.BlockCopy(NeedExByte, 0, Data, handBytes.Length, NeedExByte.Length);
                System.Buffer.BlockCopy(endBytes, 0, Data, (handBytes.Length + NeedExByte.Length), endBytes.Length);
                current = 0;
                IsDataExtraSuccess = true;
            }
            catch
            {
                IsDataExtraSuccess = false;
            }
        }


        #region 整数
        /// <summary>
        /// 读取内存流中的头2位并转换成整型
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public virtual bool ReadInt16(out short values)
        {

            try
            {
                values = BitConverter.ToInt16(Data, current);
                current = Interlocked.Add(ref current, 2);
                return true;
            }
            catch
            {
                values = 0;
                return false;
            }
        }


        /// <summary>
        /// 读取内存流中的头4位并转换成整型
        /// </summary>
        /// <param name="ms">内存流</param>
        /// <returns></returns>
        public virtual bool ReadInt32(out int values)
        {
            try
            {
                values = BitConverter.ToInt32(Data, current);
                current = Interlocked.Add(ref current, 4);
                return true;
            }
            catch(Exception ex)
            {
                values = 0;
                return false;
            }
        }


        /// <summary>
        /// 读取内存流中的头8位并转换成长整型
        /// </summary>
        /// <param name="ms">内存流</param>
        /// <returns></returns>
        public virtual bool ReadInt64(out long values)
        {
            try
            {
                values = BitConverter.ToInt64(Data, current);
                current = Interlocked.Add(ref current, 8);
                return true;
            }
            catch
            {
                values = 0;
                return false;
            }
        }

        /// <summary>
        /// 读取内存流中的首位
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public virtual bool ReadByte(out byte values)
        {
            try
            {
                values = (byte)Data[current];
                current = Interlocked.Increment(ref current);
                return true;
            }
            catch
            {
                values = 0;
                return false;
            }
        }

        #endregion

        #region 浮点数


        /// <summary>
        /// 读取内存流中的头4位并转换成单精度浮点数
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public virtual bool ReadFloat(out float values)
        {

            try
            {
                values = BitConverter.ToSingle(Data, current);
                current = Interlocked.Add(ref current, 4);
                return true;
            }
            catch
            {
                values = 0.0f;
                return false;
            }
        }


        /// <summary>
        /// 读取内存流中的头8位并转换成浮点数
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public virtual bool ReadDouble(out double values)
        {

            try
            {
                values = BitConverter.ToDouble(Data, current);
                current = Interlocked.Add(ref current, 8);
                return true;
            }
            catch
            {
                values = 0.0;
                return false;
            }
        }


        #endregion

        #region 布尔值
        /// <summary>
        /// 读取内存流中的头1位并转换成布尔值
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public virtual bool ReadBoolean(out bool values)
        {

            try
            {
                values = BitConverter.ToBoolean(Data, current);
                current = Interlocked.Add(ref current, 1);
                return true;
            }
            catch
            {
                values = false;
                return false;
            }
        }

        #endregion

        #region 字符串
        /// <summary>
        /// 读取内存流中一段字符串
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public virtual bool ReadString(out string values)
        {
            int lengt;
            try
            {
                if (ReadInt32(out lengt))
                {

                    Byte[] buf = new Byte[lengt];

                    System.Buffer.BlockCopy(Data, current, buf, 0, buf.Length);

                    values = Encoding.UTF8.GetString(buf, 0, buf.Length);

                    current = Interlocked.Add(ref current, lengt);

                    return true;

                }
                else
                {
                    values = "";
                    return false;
                }
            }
            catch
            {
                values = "";
                return false;
            }

        }
        #endregion

        #region 数据
        /// <summary>
        /// 读取内存流中一段数据
        /// </summary>
        /// <param name="ms"></param>
        /// <returns></returns>
        public virtual bool ReadByteArray(out byte[] values)
        {
            int lengt;
            try
            {
                if (ReadInt32(out lengt))
                {
                    values = new Byte[lengt];
                    System.Buffer.BlockCopy(Data, current, values, 0, values.Length);
                    current = Interlocked.Add(ref current, lengt);
                    return true;

                }
                else
                {
                    values = null;
                    return false;
                }
            }
            catch
            {
                values = null;
                return false;
            }

        }
        #endregion

        #region 对象

        /// <summary>
        /// 把字节反序列化成相应的对象
        /// </summary>
        /// <param name="pBytes">字节流</param>
        /// <returns>object</returns>
        //protected virtual T DeserializeObject<T>(byte[] pBytes)
        //{
        //    object _newOjb = null;
        //    if (pBytes == null)
        //        return (T)_newOjb;

        //    System.IO.MemoryStream _memory = new System.IO.MemoryStream(pBytes);
        //    _memory.Position = 0;
        //    BinaryFormatter formatter = new BinaryFormatter();
        //    //  formatter.TypeFormat = System.Runtime.Serialization.Formatters.FormatterTypeStyle.XsdString;
        //    _newOjb = formatter.Deserialize(_memory);
        //    _memory.Close();
        //    return (T)_newOjb;
        //}

        /// <summary>
        /// 把字节反序列化成相应的对象
        /// </summary>
        /// <param name="pBytes">字节流</param>
        ///// <returns>object</returns>
        public virtual T DeserializeObject<T>(byte[] pBytes)
        {

        //    string xml = Encoding.UTF8.GetString(pBytes);


        //    Object result = new object();
        //    XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
        //    using (Stream stream = new MemoryStream(Encoding.Unicode.GetBytes(xml)))
        //    {
        //        XmlReader xmlReader = XmlReader.Create(stream);

        //        result = (T)xmlSerializer.Deserialize(xmlReader);

        //        xmlReader.Close();

        //    }
        //    return (T)result;

            Polenter.Serialization.SharpSerializer Serializer = new Polenter.Serialization.SharpSerializer(true);
            return Serializer.Deserialize<T>(pBytes);

        }


        /// <summary>
        /// 读取一个对象
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public virtual bool ReadObject<T>(out T obj)
        {
            byte[] data;
            if (this.ReadByteArray(out data))
            {
                obj = DeserializeObject<T>(data);
                return true;
            }
            else
            {
                obj = default(T);
                return false;
            }

        }

        #endregion

    }
}
