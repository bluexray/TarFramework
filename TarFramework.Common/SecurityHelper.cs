using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace TarFramework.Common
{
    public static class SecurityHelper
    {
        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="inputdata">输入的数据</param>
        /// <param name="iv">向量128位</param>
        /// <param name="strKey">加密密钥</param>
        /// <returns></returns>
        public static byte[] EncryptAES(byte[] inputdata, byte[] iv, string strKey)
        {
            //分组加密算法   
            SymmetricAlgorithm des = Rijndael.Create();
            byte[] inputByteArray = inputdata;//得到需要加密的字节数组       
            //设置密钥及密钥向量
            des.Key = Encoding.UTF8.GetBytes(strKey.Substring(0, 32));
            des.IV = iv;
            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(inputByteArray, 0, inputByteArray.Length);
                    cs.FlushFinalBlock();
                    byte[] cipherBytes = ms.ToArray();//得到加密后的字节数组   
                    cs.Close();
                    ms.Close();
                    return cipherBytes;
                }
            }
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="inputdata">输入的数据</param>
        /// <param name="iv">向量128</param>
        /// <param name="strKey">key</param>
        /// <returns></returns>
        public static byte[] DecryptAES(byte[] inputdata, byte[] iv, string strKey)
        {
            SymmetricAlgorithm des = Rijndael.Create();
            des.Key = Encoding.UTF8.GetBytes(strKey.Substring(0, 32));
            des.IV = iv;
            byte[] decryptBytes = new byte[inputdata.Length];
            using (MemoryStream ms = new MemoryStream(inputdata))
            {
                using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cs.Read(decryptBytes, 0, decryptBytes.Length);
                    cs.Close();
                    ms.Close();
                }
            }
            return decryptBytes;
        }

        public static byte[] Base64Encrypt(byte[] inputdata, byte[] iv)
        {
            byte[] inputByteArray = inputdata;//得到需要加密的字节数组  byte[] 
            if (inputByteArray != null)
            {
                var result = Convert.ToBase64String(inputByteArray);
                return iv = Encoding.UTF8.GetBytes(result);
            }
            return iv;
        }

        public static byte[] Base64Decrypt(byte[] inputdata, byte[] iv)
        {
            byte[] inputByteArray = inputdata;//得到需要加密的字节数组  byte[] 
            if (inputByteArray != null)
            {
                iv = Convert.FromBase64String(Encoding.UTF8.GetString(inputdata));
            }
            return iv;
        }

        #region DES加密解密


        /// DES加密
        /// <param >待加密的字符串</param>
        /// <param >加密密钥,要求为8位</param>
        /// <returns>加密成功返回加密后的字符串，失败返回源串</returns>
        public static byte[] EncryptDES(byte[] data, byte[] iv, string encryptKey)
        {
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
                byte[] rgbIV = iv;
                byte[] inputByteArray = data;
                var dCSP = new DESCryptoServiceProvider();
                var mStream = new MemoryStream();
                var cStream = new CryptoStream(mStream, dCSP.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return mStream.ToArray();
            }
            catch
            {
                return data;
            }
        }

        /// DES解密
        /// <param >待解密的字符串</param>
        /// <param >解密密钥,要求为8位,和加密密钥相同</param>
        /// <returns>解密成功返回解密后的字符串，失败返源串</returns>      
        public static byte[] DecryptDES(byte[] data, byte[] iv, string decryptKey)
        {
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey.Substring(0, 8));
                byte[] rgbIV = iv;
                byte[] inputByteArray = data;
                var DCSP = new DESCryptoServiceProvider();
                var mStream = new MemoryStream();
                var cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return mStream.ToArray();
            }
            catch
            {
                //return decryptString;
                return data;
            }
        }


        #endregion
    }
}
