using WADView.Refpack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace WADView
{
    public class ArchiveFile
    {
        /*
         * for each file (40 bytes per entry)
         * 4 - unused
         * 4 - filename offset
         * 4 - filename length
         * 4 - data offset
         * 4 - file length
         * 4 - compression type ('4' for refpack)
         * 4 - decompressed size
         * 12 - null
         */

        private byte[] data;
        private bool hasBeenDecompressed;

        public string Name { get; set; }
        public bool IsCompressed { get; set; }
        public uint DecompressedSize { get; set; }
        public IArchive ParentArchive { get; set; }
        public int ArchiveOffset { get; set; }

        public uint Unknown { get; set; }

        public byte[] Data
        {
            get
            {
                // Refpack is big-endian, unlike the rest of the DWFB format
                // Therefore all memorystream operations have bigEndian set to true
                if (IsCompressed && !hasBeenDecompressed)
                {
                    List<byte> decompressedData = new List<byte>();
                    using (var memoryStream = new DWFBMemoryStream(data))
                    {
                        var refpackHeader = memoryStream.ReadBytes(2, bigEndian: true);
                        if (refpackHeader[0] != 0xFB || refpackHeader[1] != 0x10) // 0x10: LU01000C - 00010000 - large files & compressed size are not supported.
                        {
                            throw new NotRefpackException("Data was not compressed using refpack (header does not match) - possibly corrupted?");
                        }

                        memoryStream.Seek(3, SeekOrigin.Current); // Skip decompressed size

                        byte[] currentByte = memoryStream.ReadBytes(1, bigEndian: true);

                        var commands = new List<IRefpackCommand>();
                        var commandCount = new Dictionary<Type, int>();

                        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
                        {
                            if (type.GetInterfaces().Contains(typeof(IRefpackCommand)))
                            {
                                commands.Add((IRefpackCommand)Activator.CreateInstance(type));
                            }
                        }

                        while (memoryStream.Position < data.Length)
                        {
                            foreach (var command in commands)
                            {
                                if (command.OpcodeMatches(currentByte[0]))
                                {
                                    var commandType = command.GetType();

                                    if (commandCount.ContainsKey(commandType))
                                        commandCount[commandType]++;
                                    else
                                        commandCount.Add(commandType, 1);

                                    try
                                    {
                                        command.Decompress(data, ref decompressedData, (int)memoryStream.Position - 1, out uint skipAhead);
                                        memoryStream.Seek(command.Length + skipAhead - 1, SeekOrigin.Current);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Uh oh:\n{ex}");
                                    }

                                    if (command.StopAfterFound) 
                                        memoryStream.Seek(data.Length, SeekOrigin.Current); // stop
                                }
                            }
                            currentByte = memoryStream.ReadBytes(1, bigEndian: true);
                        }
                    }

                    // Avoid decompressing after we've already done it once! Store the result in case its used later.
                    hasBeenDecompressed = true;
                    data = decompressedData.ToArray();
                }
                return data;
            }
            set
            {
                data = value;
            }
        }
    }
}
