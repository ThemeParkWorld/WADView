using System.Collections.Generic;

namespace WADView.Refpack
{
    public class ThreeByteCommand : IRefpackCommand
    {
        public int Length => 3;
        public bool StopAfterFound => false;
        public void Decompress(byte[] data, ref List<byte> decompressedData, int offset, out uint skipAhead)
        {
            uint proceedingDataLength = (uint)((data[offset + 1] & 0xC0) >> 6);
            uint referencedDataLength = (uint)((data[offset] & 0x3F) + 4);
            uint referencedDataDistance = (uint)(((data[offset + 1] & 0x3F) << 8) + data[offset + 2] + 1);
            skipAhead = proceedingDataLength;

            RefpackUtils.DecompressData(data, ref decompressedData, offset, Length, proceedingDataLength, referencedDataLength, referencedDataDistance);
        }
        public bool OpcodeMatches(byte firstByte) => firstByte.GetBits(0, 1).ValuesEqual(new[] { true, false });
    }
}
