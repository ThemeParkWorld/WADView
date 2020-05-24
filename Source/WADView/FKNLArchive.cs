using System;
using System.Collections.Generic;
using System.IO;

namespace WADView
{
    /*
     * The 'FKNL' WAD format isn't actually used in the PC game,
     * but it's used within the PS2 (and probably PS1) ports of
     * the game.  It's quite similar to DWFB in many ways (it
     * uses refpack, for example) which meas that FKNLArchive
     * and DWFBArchive are technically compatible with each
     * other - as long as they use their separate loaders.
     */

    // TODO
    public class FKNLArchive : IArchive
    {
        private DWFBMemoryStream memoryStream;
        private int version;
        public byte[] buffer { get; internal set; }
        public List<ArchiveFile> files { get; internal set; }

        public FKNLArchive(string path)
        {
            LoadArchive(path);
        }

        public void Dispose()
        {
            memoryStream.Dispose();
        }

        private void ReadArchive()
        {
            memoryStream.Seek(0, SeekOrigin.Begin);
            /* Header:
             * 4 - magic number ('FKNL')
             * 4 - flags
             * 4 - data offset
             *  if flags & 2
             *      4 - size
             *      4 - zsize
             *      4 - files
             *      4 - dummy
             * 4 - dummy
             * 4 - filename dictionary offset
             * 4 - ??
             * 4 - ??
             * 4 - ??
             */

            /* Details directory
             * for each file:
             * 4 - filename size?
             * 4 - filename offset
             * 4 - file offset (relative to file data start)
             * 4 - file length
             */

            var magicNumber = memoryStream.ReadString(4);
            if (magicNumber != "FKNL") throw new NotDWFBException($"Magic number did not match: {magicNumber}");
            version = memoryStream.ReadInt32();

            var firstFileOffset = memoryStream.ReadInt32();

            var fileDirectoryOffset = memoryStream.ReadInt32();
            var fileDirectoryLength = memoryStream.ReadInt32();

            memoryStream.Seek(4, SeekOrigin.Current); // Skip 'null'

            // Details directory
            // See DWFBFile for more info
            for (int i = 0; i < 1; ++i)
            {
                GC.Collect();
                // Save the current position so that we can go back to it later
                var initialPos = memoryStream.Position;

                ArchiveFile newFile = new ArchiveFile();
                var filenameLength = memoryStream.ReadUInt32();
                var filenameOffset = memoryStream.ReadUInt32();
                var dataOffset = memoryStream.ReadUInt32();
                var dataLength = memoryStream.ReadUInt32();

                newFile.IsCompressed = memoryStream.ReadUInt32() == 4;
                newFile.DecompressedSize = memoryStream.ReadUInt32();

                // Set file's name
                memoryStream.Seek(filenameOffset, SeekOrigin.Begin);
                newFile.Name = memoryStream.ReadString((int)filenameLength);

                // Get file's raw data
                memoryStream.Seek(dataOffset, SeekOrigin.Begin);
                newFile.Data = memoryStream.ReadBytes((int)dataLength);

                newFile.ArchiveOffset = (int)dataOffset;
                newFile.ParentArchive = this;

                files.Add(newFile);
                memoryStream.Seek(initialPos + 40, SeekOrigin.Begin); // Return to initial position, skip to the next file's data
            }
        }

        public void LoadArchive(string path)
        {
            // Set up read buffer
            var tempStreamReader = new StreamReader(path);
            var fileLength = (int)tempStreamReader.BaseStream.Length;
            buffer = new byte[fileLength];
            tempStreamReader.BaseStream.Read(buffer, 0, fileLength);
            tempStreamReader.Close();

            memoryStream = new DWFBMemoryStream(buffer);
            files = new List<ArchiveFile>();

            ReadArchive();
        }
    }
}
