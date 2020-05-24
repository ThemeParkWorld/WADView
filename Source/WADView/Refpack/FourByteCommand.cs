using System.Collections.Generic;

namespace WADView.Refpack
{
    public class FourByteCommand : IRefpackCommand
    {
        public int Length => 4;
        public bool StopAfterFound => false;
        public void Decompress(byte[] data, ref List<byte> decompressedData, int offset, out uint skipAhead)
        {
            uint proceedingDataLength = (uint)((data[offset] & 0x03));
            uint referencedDataLength = (uint)(((data[offset] & 0x0C) << 6) + data[offset + 3] + 5);
            uint referencedDataDistance = (uint)(((data[offset] & 0x10) << 12) + (data[offset + 1] << 8) + data[offset + 2] + 1);
            skipAhead = proceedingDataLength;

            RefpackUtils.DecompressData(data, ref decompressedData, offset, Length, proceedingDataLength, referencedDataLength, referencedDataDistance);
        }
        public bool OpcodeMatches(byte firstByte) => firstByte.GetBits(0, 1, 2).ValuesEqual(new[] { true, true, false });
    }
}
