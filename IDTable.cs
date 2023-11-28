using Syroot.BinaryData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTDataSQLiteConverter
{
    internal class IDTable
    {
        private readonly uint ExpectedMagic = 0x42444449;

        private readonly SortedDictionary<ulong, long> IDs = new();
        public void Read(string indexfn)
        {
            var fs = new FileStream(indexfn, FileMode.Open);
            var bs = new BinaryStream(fs);

            var magic = bs.ReadUInt32();
            if (magic != ExpectedMagic)
            {
                throw new InvalidDataException("Input db_str is not a STDB string database.");
            }

            var count = bs.ReadUInt32();
            for (int i = 0; i < count; i++)
            {
                ulong hash = bs.ReadUInt64();
                long strIndex = bs.ReadInt64();
                IDs.Add(hash, strIndex);
            }
        }

        public long GetStringIndex(ulong hash)
        {
            if (IDs.TryGetValue(hash, out long index))
                return index;

            return -1;
        }
    }
}
