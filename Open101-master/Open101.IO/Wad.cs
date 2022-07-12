using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Open101.IO
{
    public class Wad
    {
        public struct FileRecord
        {
            public uint m_offset;
            public uint m_size;
            public uint m_compressedSize;
            public bool m_isCompressed;
            public uint m_CRC;
            public string m_filename;

            public bool IsBlank => m_offset == 0 && m_size == 0 && m_compressedSize == 0 && m_isCompressed == false && m_CRC == 0;
        }

        public readonly string m_file;
        private readonly FileRecord[] m_records;
        public readonly Dictionary<string, FileRecord> m_recordDict;
        
        public Wad(string file)
        {
            m_file = file;
            
            using var stream = File.OpenRead(file);
            var records = ReadHeader(stream);

            m_records = records;
            m_recordDict = new Dictionary<string, FileRecord>();
            foreach (FileRecord record in records)
            {
                m_recordDict[record.m_filename] = record;
            }
        }

        public static FileRecord[] ReadHeader(Stream stream)
        {
            using var reader = new BinaryReader(stream);

            Span<byte> headerSpan = stackalloc byte[5];
            reader.Read(headerSpan);

            if (headerSpan[0] != 'K' ||
                headerSpan[1] != 'I' ||
                headerSpan[2] != 'W' ||
                headerSpan[3] != 'A' ||
                headerSpan[4] != 'D')
            {
                throw new InvalidDataException("invalid wad header");
            }
            
            uint version = reader.ReadUInt32();
            uint numFiles = reader.ReadUInt32();

            if (version >= 2)
            {
                reader.ReadByte(); // unk?
            }

            var records = new FileRecord[numFiles];
            for (int i = 0; i < numFiles; i++)
            {
                records[i].m_offset = reader.ReadUInt32();
                records[i].m_size = reader.ReadUInt32();
                records[i].m_compressedSize = reader.ReadUInt32();
                records[i].m_isCompressed = reader.ReadBoolean();
                records[i].m_CRC = reader.ReadUInt32();
                int nameLength = reader.ReadInt32();
                records[i].m_filename = new string(reader.ReadChars(nameLength - 1));
                var string0 = reader.ReadByte(); // 0 at end of string
                Debug.Assert(string0 == 0);
            }
            return records;
        }

        public Stream OpenFile(string file)
        {
            if (!m_recordDict.TryGetValue(file, out var record))
            {
                return null;
            }
            
            using var fileStream = File.OpenRead(m_file);
            fileStream.Position = record.m_offset;
            var data = new byte[record.m_size];
            
            if (record.m_isCompressed)
            {
                fileStream.Position += 2; // skip zlib header
                using DeflateStream decompressor = new DeflateStream(fileStream, CompressionMode.Decompress);
                decompressor.Read(data, 0, (int)record.m_size);
            } else
            {
                fileStream.Read(data, 0, (int) record.m_size);
            }
            return new MemoryStream(data);
        }
    }
}