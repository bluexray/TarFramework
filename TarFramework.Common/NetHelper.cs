using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using TarFramework.Common;

namespace TarFramework.T_araNet
{
    /// <summary>
    /// NetHelper 。
    /// </summary>
    public static class NetHelper
    {
        #region IsPublicIPAddress
        public static bool IsPublicIPAddress(string ip)
        {
            if (ip.StartsWith("10.")) //A类 10.0.0.0到10.255.255.255.255 
            {
                return false;
            }

            if (ip.StartsWith("172."))//B类 172.16.0.0到172.31.255.255 
            {
                if (ip.Substring(6, 1) == ".")
                {
                    int secPart = int.Parse(ip.Substring(4, 2));
                    if ((16 <= secPart) && (secPart <= 31))
                    {
                        return false;
                    }
                }
            }

            if (ip.StartsWith("192.168."))//C类 192.168.0.0到192.168.255.255 
            {
                return false;
            }

            return true;
        }
        #endregion

        #region ReceiveData
        /// <summary>
        /// ReceiveData 从网络读取指定长度的数据
        /// </summary>	
        public static byte[] ReceiveData(NetworkStream stream, int size)
        {
            byte[] result = new byte[size];

            NetHelper.ReceiveData(stream, result, 0, size);

            return result;
        }

        /// <summary>
        /// ReceiveData 从网络读取指定长度的数据 ，存放在buff中offset处
        /// </summary>	
        public static void ReceiveData(NetworkStream stream, byte[] buff, int offset, int size)
        {
            int readCount = 0;
            int totalCount = 0;
            int curOffset = offset;

            while (totalCount < size)
            {
                int exceptSize = size - totalCount;
                readCount = stream.Read(buff, curOffset, exceptSize);
                if (readCount == 0)
                {
                    throw new Exception("NetworkStream Interruptted !");
                }
                curOffset += readCount;
                totalCount += readCount;
            }
        }

        /// <summary>
        /// ReceiveData 从网络读取指定长度的数据
        ///// </summary>	
        //public static byte[] ReceiveData(ISafeNetworkStream stream, int size)
        //{
        //    byte[] result = new byte[size];

        //    NetHelper.ReceiveData(stream, result, 0, size);

        //    return result;
        //}

        /// <summary>
        /// ReceiveData 从网络读取指定长度的数据 ，存放在buff中offset处
        /// </summary>		
        //public static void ReceiveData(ISafeNetworkStream stream, byte[] buff, int offset, int size)
        //{
        //    int readCount = 0;
        //    int totalCount = 0;
        //    int curOffset = offset;

        //    while (totalCount < size)
        //    {
        //        int exceptSize = size - totalCount;
        //        readCount = stream.Read(buff, curOffset, exceptSize);
        //        if (readCount == 0)
        //        {
        //            throw new Exception("NetworkStream Interruptted !");
        //        }
        //        curOffset += readCount;
        //        totalCount += readCount;
        //    }
        //}
        #endregion

        #region GetRemotingHanler
        //前提是已经注册了remoting通道
        public static object GetRemotingHanler(string channelTypeStr, string ip, int port, string remotingServiceName, Type destInterfaceType)
        {
            try
            {
                string remoteObjUri = string.Format("{0}://{1}:{2}/{3}", channelTypeStr, ip, port, remotingServiceName);
                return Activator.GetObject(destInterfaceType, remoteObjUri);
            }
            catch
            {
                return null;
            }
        }
        #endregion

        #region GetLocalIp
        /// <summary>
        /// GetLocalIp 获取本机的局域网IP地址
        /// </summary>       
        public static IPAddress[] GetLocalIp()
        {
            //获取本机的IP列表,IP列表中的第一项是局域网IP，第二项是广域网IP
            IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;

            //如果本机IP列表为空，则返回空字符串
            if (addressList.Length < 1)
            {
                return null;
            }

            //返回本机的局域网IP
            return addressList;
        }


        public static IPAddress GetFirstLocalIp()
        {
            //获取本机的IP列表,IP列表中的第一项是局域网IP，第二项是广域网IP
            var addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;

            //如果本机IP列表为空，则返回空字符串
            if (addressList.Length < 1)
            {
                return null;
            }

            //返回本机的局域网IP
            return addressList[0];
        }

        /// <summary>
        /// GetLocalPublicIp 获取本机的公网IP地址
        /// </summary>       
        public static string GetLocalPublicIp()
        {
            //获取本机的IP列表,IP列表中的第一项是局域网IP，第二项是广域网IP
            IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;

            //如果本机IP列表小于2，则返回空字符串
            if (addressList.Length < 2)
            {
                return "";
            }

            //返回本机的广域网IP
            return addressList[1].ToString();

        }
        #endregion

        #region IsConnectedToInternet
        /// <summary>
        /// IsConnectedToInternet 机器是否联网
        /// </summary>       
        public static bool IsConnectedToInternet()
        {
            int Desc = 0;
            return InternetGetConnectedState(Desc, 0);
        }

        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(int Description, int ReservedValue);
        #endregion

        #region GetMacAddress 获取网卡mac地址
        /// <summary>
        /// GetMacAddress 获取本机所有网卡的Mac地址
        /// </summary>       
        public static IList<string> GetMacAddress()
        {
            return MachineHelper.GetMacAddress();
        }
        #endregion

        #region DownLoadFileFromUrl
        /// <summary>
        /// DownLoadFileFromUrl 将url处的文件下载到本地
        /// </summary>       
        public static void DownLoadFileFromUrl(string url, string saveFilePath)
        {
            FileStream fstream = new FileStream(saveFilePath, FileMode.Create, FileAccess.Write);
            WebRequest wRequest = WebRequest.Create(url);

            try
            {
                WebResponse wResponse = wRequest.GetResponse();
                int contentLength = (int)wResponse.ContentLength;

                byte[] buffer = new byte[1024];
                int read_count = 0;
                int total_read_count = 0;
                bool complete = false;

                while (!complete)
                {
                    read_count = wResponse.GetResponseStream().Read(buffer, 0, buffer.Length);

                    if (read_count > 0)
                    {
                        fstream.Write(buffer, 0, read_count);
                        total_read_count += read_count;
                    }
                    else
                    {
                        complete = true;
                    }
                }

                fstream.Flush();
            }
            finally
            {
                fstream.Close();
                wRequest = null;
            }
        }
        #endregion

        #region 将字符串形式的IP地址转换成IPAddress对象
        /// <summary>
        /// 将字符串形式的IP地址转换成IPAddress对象
        /// </summary>
        /// <param name="ip">字符串形式的IP地址</param>        
        public static IPAddress StringToIpAddress(string ip)
        {
            return IPAddress.Parse(ip);
        }
        #endregion

        public static string GetLocalPcName()
        {
            return Dns.GetHostName();
        }
    }
}
