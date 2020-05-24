﻿using System;
using System.IO;
using System.Text;

namespace WADView
{
    public class DWFBMemoryStream : MemoryStream
    {
        public DWFBMemoryStream(byte[] buffer) : base(buffer) { }

        public byte[] ReadBytes(int length, bool bigEndian = false)
        {
            byte[] bytes = new byte[length];
            Read(bytes, 0, length);

            if (bigEndian) Array.Reverse(bytes);
            return bytes;
        }

        public string ReadString(int length, bool bigEndian = false)
        {
            return Encoding.ASCII.GetString(ReadBytes(length, bigEndian));
        }

        public int ReadInt32(bool bigEndian = false)
        {
            return BitConverter.ToInt32(ReadBytes(4, bigEndian), 0);
        }

        public uint ReadUInt32(bool bigEndian = false)
        {
            return BitConverter.ToUInt32(ReadBytes(4, bigEndian), 0);
        }
    }
}
