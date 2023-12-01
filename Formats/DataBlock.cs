using Syroot.BinaryData;

using System;
using System.Buffers.Binary;

namespace GTDataSQLiteConverter
{
    public class DataBlock
    {
        private readonly uint ExpectedMagic = 0x54445447;
        public const uint HeaderSize = 0x10;

        public ushort Version { get; set; }
        public short TableID { get; set; }

        /// <summary>
        /// AKA Number of rows
        /// </summary>
        public ushort NumOfElements { get; set; }

        /// <summary>
        /// AKA Row length or Block size
        /// </summary>
        public ushort ElementSize { get; set; }

        public byte[] Buffer { get; set; }

        public void Read(BinaryStream bs)
        {
            var magic = bs.ReadUInt32();
            if (magic != ExpectedMagic)
                throw new InvalidDataException("DB data is not a GTDT table.");

            Version = bs.ReadUInt16();
            TableID = bs.ReadInt16();
            NumOfElements = bs.ReadUInt16();
            ElementSize = bs.ReadUInt16(); // Also BlockSize
            uint blockNumber = bs.ReadUInt32(); // Aka file size
            Buffer = bs.ReadBytes((int)blockNumber - 0x10);
        }

        public void Write(Stream stream)
        {
            BinaryStream bs = new BinaryStream(stream, ByteConverter.Little);
            bs.WriteUInt32(ExpectedMagic);
            bs.WriteUInt16(Version);
            bs.WriteInt16(TableID);
            bs.WriteUInt16(NumOfElements);
            bs.WriteUInt16(ElementSize);
            bs.WriteUInt32((uint)(HeaderSize + (NumOfElements * ElementSize)));

            if (Buffer is not null)
                bs.Write(Buffer);
        }

        public Span<byte> GetEntry(int index)
        {
            return Buffer.AsSpan(index * ElementSize, ElementSize);
        }

        public long SearchEntry(ulong hash)
        {
            int min = 0;
            int max = NumOfElements - 1;
            while (min <= max)
            {
                int mid = min + (max - min) / 2;

                ulong currentHash = BinaryPrimitives.ReadUInt64LittleEndian(Buffer.AsSpan(mid * ElementSize, ElementSize));
                if (currentHash == hash)
                    return mid;

                if (currentHash < hash)
                    min = mid + 1;
                else
                    max = mid - 1;
            }

            return -1;
        }
    }
}
