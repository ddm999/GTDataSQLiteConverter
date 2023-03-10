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

        private readonly SortedDictionary<ulong, string> IDs = new();
        public void Read(string indexfn, string stringfn)
        {
            StringTable stringTable = new();
            stringTable.Read(stringfn);

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
                ulong id = bs.ReadUInt64();
                ushort num = (ushort)bs.ReadUInt64();
                IDs.Add(id, stringTable.Get(num));
            }
        }

        public string? Get(ulong id) => IDs.TryGetValue(id, out string? value) ? value : null;
    }
}
