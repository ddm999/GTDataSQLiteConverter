using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace GTDataSQLiteConverter.Formats
{
    public class CarColors
    {
        /// <summary>
        /// "GT2K"
        /// </summary>
        public const uint ExpectedMagic = 0x4B325447;

        public List<CarColor> Colors { get; set; }

        public void Read(string indexfn)
        {
            var fs = new FileStream(indexfn, FileMode.Open);
            var bs = new BinaryStream(fs);

            var magic = bs.ReadUInt32();
            if (magic != ExpectedMagic)
            {
                throw new InvalidDataException("Input db_str is not a STDB string database.");
            }

            var carCount = bs.ReadUInt32();
            var carToColorsMapOffset = bs.ReadUInt32();
            var colorListOffset = bs.ReadUInt32();
            var fileSize = bs.ReadUInt32();

            bs.Position = carToColorsMapOffset;
            for (int i = 0; i < carCount; i++)
            {
                // TODO
            }

            bs.Position = colorListOffset;
            uint colorCount = bs.ReadUInt32();

            for (int i = 0; i < colorCount; i++)
            {
                var color = new CarColor();
                color.Read(bs);
                Colors.Add(color);
            }
        }

        public class CarColor
        {
            public uint ColorID { get; set; }
            public uint LatinNameIndex { get; set; }
            public uint JapaneseNameIndex { get; set; }
            public uint ThumbnailColor { get; set; }

            public void Read(BinaryStream bs)
            {
                ColorID = bs.ReadUInt32();
                LatinNameIndex = bs.ReadUInt32();
                JapaneseNameIndex = bs.ReadUInt32();
                ThumbnailColor = bs.ReadUInt32();
            }
        }

        public void Write(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
