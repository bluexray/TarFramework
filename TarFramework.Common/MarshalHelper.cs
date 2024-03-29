﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TarFramework.Common
{
    public  static class MarshalHelper
    {
        public static T GetObject<T>(byte[] data, int size)
        {
            Contract.Assume(size == Marshal.SizeOf(typeof(T)));
            IntPtr pnt = Marshal.AllocHGlobal(size);

            try
            {
                // Copy the array to unmanaged memory.
                Marshal.Copy(data, 0, pnt, size);
                return (T)Marshal.PtrToStructure(pnt, typeof(T));
            }
            finally
            {
                // Free the unmanaged memory.
                Marshal.FreeHGlobal(pnt);
            }
        }

        public static byte[] GetData(object obj)
        {
            var size = Marshal.SizeOf(obj.GetType());
            var data = new byte[size];
            IntPtr pnt = Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(obj, pnt, true);
                // Copy the array to unmanaged memory.
                Marshal.Copy(pnt, data, 0, size);
                return data;
            }
            finally
            {
                // Free the unmanaged memory.
                Marshal.FreeHGlobal(pnt);
            }
        }

        public static T ReadMarshal<T>(this System.IO.BinaryReader reader)
        {
            var length = Marshal.SizeOf(typeof(T));
            var data = reader.ReadBytes(length);
            return GetObject<T>(data, data.Length);
        }

        public static void WriteMarshal<T>(this System.IO.BinaryWriter writer, T obj)
        {
            writer.Write(GetData(obj));
        }
    }
}
