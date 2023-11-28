using Syroot.BinaryData;

using System;
using System.Buffers.Binary;

namespace GTDataSQLiteConverter
{
    enum ParamDBStringType
    {
        ID = 0,
        String,
        Unicode,
        Color
    }

    public class DataBlock
    {
        private readonly uint ExpectedMagic = 0x54445447;

        public uint Version { get; set; }
        public uint TableID { get; set; }

        /// <summary>
        /// AKA Number of rows
        /// </summary>
        public ushort NumOfElements { get; set; }

        /// <summary>
        /// AKA Row length or Block size
        /// </summary>
        public ushort ElementSize { get; set; }

        /// <summary>
        /// AKA File size
        /// </summary>
        public uint BlockNumber { get; set; }

        public byte[] Buffer { get; set; }

        public void Read(BinaryStream bs)
        {
            var magic = bs.ReadUInt32();
            if (magic != ExpectedMagic)
                throw new InvalidDataException("DB data is not a GTDT table.");

            Version = bs.ReadUInt16();
            TableID = bs.ReadUInt16();
            NumOfElements = bs.ReadUInt16();
            ElementSize = bs.ReadUInt16(); // Also BlockSize
            BlockNumber = bs.ReadUInt32();
            Buffer = bs.ReadBytes((int)BlockNumber - 0x10);
        }

        public Span<byte> GetEntry(int index)
        {
            return Buffer.AsSpan(index * ElementSize, ElementSize);
        }

        public long SearchEntry(ulong hash)
        {
            int min = -1;
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
