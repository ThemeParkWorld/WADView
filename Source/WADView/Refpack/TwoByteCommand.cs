using System.Collections.Generic;

namespace WADView.Refpack
{
    public class TwoByteCommand : IRefpackCommand
    {
        public int Length => 2;
        public bool StopAfterFound => false;
        public void Decompress(byte[] data, ref List<byte> decompressedData, int offset, out uint skipAhead)
        {
            uint proceedingDataLength = (uint)((data[offset] & 0x03));
            uint referencedDataLength = (uint)(((data[offset] & 0x1C) >> 2) + 3);
            uint referencedDataDistance = (uint)(((data[offset] & 0x60) << 3) + data[offset + 1] + 1);
            skipAhead = proceedingDataLength;

            RefpackUtils.DecompressData(data, ref decompressedData, offset, Length, proceedingDataLength, referencedDataLength, referencedDataDistance);
        }
        public bool OpcodeMatches(byte firstByte) => !firstByte.GetBit(0);
    }
}
