using Syroot.BinaryData;
using System.Text;

namespace GTDataSQLiteConverter
{
    internal class StringTable
    {
        private readonly uint ExpectedMagic = 0x42445453;

        public List<string> Strings = new();
        public void Read(string filename)
        {
            var fs = new FileStream(filename, FileMode.Open);
            var bs = new BinaryStream(fs);

            var magic = bs.ReadUInt32();
            if (magic != ExpectedMagic)
            {
                throw new InvalidDataException("Input db_str is not a STDB string database.");
            }

            var count = bs.ReadUInt32();
            var encodingNum = bs.ReadUInt32();

            Encoding encoding = Encoding.Default;
            if (encodingNum == 0xFFFF)
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                encoding = Encoding.GetEncoding("euc-jp");
            }
            else if (encodingNum != 0x0001)
            {
                throw new InvalidDataException("STDB contains unknown string encoding type.");
            }

            if (fs.Length != bs.ReadUInt32())
            {
                Console.WriteLine("Warning: STDB has bad file length.");
            }

            long basepos = bs.Position;
            for (int i = 0; i < count; i++)
            {
                bs.Position = basepos + (0x4 * i);
                uint strpos = bs.ReadUInt32();
                bs.Position = strpos;
                ushort stringLength = bs.ReadUInt16();
                byte[] stringBytes = new byte[stringLength];
                bs.Read(stringBytes);
                Strings.Add(encoding.GetString(stringBytes).TrimEnd('\0'));
            }
        }

        public string? Get(ushort index)
        {
            if (index >= Strings.Count)
                return null;
            return Strings[index];
        }
    }
}
