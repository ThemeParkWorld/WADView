using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace WADView
{
    /*
     * Special thanks to Fatbag: http://wiki.niotso.org/RefPack, 
     * WATTO: http://wiki.xentax.com/index.php/WAD_DWFB, 
     * and Rhys: https://github.com/RHY3756547/FreeSO/blob/master/TSOClient/tso.files/FAR3/Decompresser.cs.
     */
    public class DWFBArchive : IArchive
    {
        private DWFBMemoryStream memoryStream;
        private int version;
        private byte[] buffer { get; set; }

        public List<ArchiveFile> files { get; private set; }

        public DWFBArchive(string path)
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
             * 4 - magic number ('DWFB')
             * 4 - version
             * 64 - padding??
             * 4 - file count
             * 4 - file list offset
             * 4 - file list length
             * 4 - null
             */

            var magicNumber = memoryStream.ReadString(4);
            if (magicNumber != "DWFB") throw new NotDWFBException($"Magic number did not match: {magicNumber}");
            version = memoryStream.ReadInt32();
            memoryStream.Seek(64, SeekOrigin.Current); // Skip padding
            var fileCount = memoryStream.ReadInt32();

            var fileDirectoryOffset = memoryStream.ReadInt32();
            var fileDirectoryLength = memoryStream.ReadInt32();

            memoryStream.Seek(4, SeekOrigin.Current); // Skip 'null'

            uint lowestUnk = uint.MaxValue;
            // Details directory
            // See DWFBFile for more info
            for (int i = 0; i < fileCount; ++i)
            {
                GC.Collect();
                // Save the current position so that we can go back to it later
                var initialPos = memoryStream.Position;

                ArchiveFile newFile = new ArchiveFile();

                var unknown = memoryStream.ReadUInt32();
                if (unknown < lowestUnk)
                    lowestUnk = unknown;
                
                var filenameOffset = memoryStream.ReadUInt32();
                var filenameLength = memoryStream.ReadUInt32();
                var dataOffset = memoryStream.ReadUInt32();
                var dataLength = memoryStream.ReadUInt32();

                newFile.IsCompressed = memoryStream.ReadUInt32() == 4;
                newFile.DecompressedSize = memoryStream.ReadUInt32();

                // Set file's name
                memoryStream.Seek(filenameOffset, SeekOrigin.Begin);
                newFile.Name = memoryStream.ReadString((int)filenameLength).Replace("\0", "");

                // Get file's raw data
                memoryStream.Seek(dataOffset, SeekOrigin.Begin);
                newFile.Data = memoryStream.ReadBytes((int)dataLength);

                newFile.ArchiveOffset = (int)dataOffset;
                newFile.ParentArchive = this;

                newFile.Unknown = unknown;

                files.Add(newFile);
                memoryStream.Seek(initialPos + 40, SeekOrigin.Begin); // Return to initial position, skip to the next file's data
            }

            MessageBox.Show($"Lowest unknown: {lowestUnk}");
        }

        public void WriteArchive(BinaryWriter binaryWriter)
        {
            /* Write header:
             * 4 bytes: DWFB                0
             * 4 bytes: version             4
             * 64 bytes: padding            8
             * 4 bytes: file count          72
             * 4 bytes: file list offset    76
             * 4 bytes: file list length    80
             * 4 bytes: padding             84
             */

            binaryWriter.Write(new char[] { 'D', 'W', 'F', 'B' }); // DWFB
            binaryWriter.Write(2); // Version

            for (int i = 0; i < 16; ++i)
                binaryWriter.Write(0x00); // Padding

            binaryWriter.Write(files.Count); // File count
            binaryWriter.Write(0xEE); // File list offset
            binaryWriter.Write(0xAA); // File list length
            binaryWriter.Write(0x00); // Padding

            /* Write file directory (placeholder values):
             * 4 bytes: unknown
             * 4 bytes: filename offset
             * 4 bytes: filename length
             * 4 bytes: data offset
             * 4 bytes: file length
             * 4 bytes: compressed (4 for refpack, 0 for not compressed)
             * 4 bytes: decompressed size
             * 12 bytes: padding
             */
            // TODO: Compress files?

            List<long> fileDirectoryOffsets = new List<long>();

            foreach (var file in files)
            {
                fileDirectoryOffsets.Add(binaryWriter.BaseStream.Position);

                binaryWriter.Write(file.Unknown); // Unknown
                binaryWriter.Write(0xFF); // Filename offset
                binaryWriter.Write(file.Name.Length + 1); // Filename length
                binaryWriter.Write(0xFF); // Data offset
                binaryWriter.Write(file.Data.Length); // File length
                binaryWriter.Write(0x00); // Compression flag (uncompressed = 00)
                binaryWriter.Write(0x00); // Decompressed size (not compressed, so size = 0)

                for (int i = 0; i < 3; ++i)
                    binaryWriter.Write(0x00);
            }

            // Write file data
            int fileIndex = 0;
            foreach (var file in files)
            {
                binaryWriter.Write(file.Data);

                // Write data offset
                var lastPos = binaryWriter.BaseStream.Position;
                binaryWriter.BaseStream.Seek(fileDirectoryOffsets[fileIndex++] + 12, SeekOrigin.Begin);

                binaryWriter.Write((int)lastPos - file.Data.Length);

                binaryWriter.BaseStream.Seek(lastPos, SeekOrigin.Begin);
            }

            /* Write file directory:
             * n bytes: null-terminated filename
             */

            var listStartPos = binaryWriter.BaseStream.Position;
            fileIndex = 0;
            foreach (var file in files)
            {
                binaryWriter.Write(Encoding.ASCII.GetBytes(file.Name));
                binaryWriter.Write(new byte[] {0x00});

                // Now write filename metadata
                var lastPos = binaryWriter.BaseStream.Position;
                binaryWriter.BaseStream.Seek(fileDirectoryOffsets[fileIndex++] + 4, SeekOrigin.Begin);

                binaryWriter.Write((int)lastPos - file.Name.Length - 1);

                binaryWriter.BaseStream.Seek(lastPos, SeekOrigin.Begin);
            }

            // File list complete, write data to header
            var currentPos = binaryWriter.BaseStream.Position;
            binaryWriter.BaseStream.Seek(76, SeekOrigin.Begin);
            binaryWriter.Write((int)listStartPos);
            binaryWriter.Write((int)(currentPos - listStartPos));
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

        public void SaveArchive(string path)
        {
            var tempStreamWriter = new StreamWriter(path);

            WriteArchive(new BinaryWriter(tempStreamWriter.BaseStream));

            tempStreamWriter.Close();
        }
    }
}

