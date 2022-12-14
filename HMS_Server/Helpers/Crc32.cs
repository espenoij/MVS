using Crc;
using System.Text;
using System;

namespace HMS_Server.Helpers
{
    internal class Crc32 : Crc32Base
    {
        public Crc32() : base(0x04C11DB7, 0xFFFFFFFF, 0xFFFFFFFF, true, true)
        {
        }

        public uint GetHash(string data)
        {
            Crc32 crc = new Crc32();

            byte[] bytes = Encoding.ASCII.GetBytes(data);
            byte[] resultBytes = crc.ComputeHash(bytes);

            return BitConverter.ToUInt32(resultBytes, 0);
        }
    }
}
