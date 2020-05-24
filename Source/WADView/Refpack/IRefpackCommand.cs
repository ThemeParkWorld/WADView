using System.Collections.Generic;

namespace WADView.Refpack
{
    public interface IRefpackCommand
    {
        bool StopAfterFound { get; }
        int Length { get; }
        void Decompress(byte[] data, ref List<byte> decompressedData, int offset, out uint skipAhead);
        bool OpcodeMatches(byte firstByte);
    }

}
