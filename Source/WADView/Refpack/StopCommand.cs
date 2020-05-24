using System.Collections.Generic;

namespace WADView.Refpack
{
    public class StopCommand : IRefpackCommand
    {
        public int Length => 1;
        public bool StopAfterFound => true;
        public void Decompress(byte[] data, ref List<byte> decompressedData, int offset, out uint skipAhead)
        {
            uint proceedingDataLength = (uint)((data[offset] & 0x03));
            skipAhead = proceedingDataLength;
            RefpackUtils.DecompressData(data, ref decompressedData, offset, Length, proceedingDataLength, 0, 0);
        }
        public bool OpcodeMatches(byte firstByte) => ((firstByte & 0x1F) + 1) << 2 > 0x70 && firstByte.GetBits(0, 1, 2).ValuesEqual(new[] { true, true, true });
    }
}
