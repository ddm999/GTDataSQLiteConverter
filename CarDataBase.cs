using GTDataSQLiteConverter.Formats;
using Microsoft.Data.Sqlite;
using Syroot.BinaryData;

namespace GTDataSQLiteConverter
{
    public class CarDataBase
    {
        private readonly uint ExpectedMagic = 0x52415447;

        public List<DataBlock> Elements { get; set; } = new();
        public StringsDataBase StringDataBase { get; set; } = new();
        public StringsDataBase UnicodeStringDataBase { get; set; } = new();

        public IDTable IDTable { get; set; } = new();
        public StringsDataBase IDStringDataBase { get; set; } = new();

        public void InitSubDatabases(string databaseFile, string stringDatabaseName, string uniStrDatabaseName)
        {
            ReadDatabase(databaseFile);
            ReadStringDatabase(stringDatabaseName);
            ReadUniStringDatabase(uniStrDatabaseName);
        }

        public void ReadDatabase(string fileName)
        {
            using var fs = new FileStream(fileName, FileMode.Open);
            var bs = new BinaryStream(fs);

            var magic = bs.ReadUInt32();
            if (magic != ExpectedMagic)
            {
                throw new InvalidDataException("Input paramdb is not a GTAR archive.");
            }

            uint numFiles = bs.ReadUInt32();
            uint indexSize = bs.ReadUInt32();
            uint alignMask = bs.ReadUInt32();

            uint[] indices = bs.ReadUInt32s((int)numFiles + 1);

            Elements = new List<DataBlock>((int)numFiles);
            for (int i = 0; i < numFiles; i++)
            {
                uint fileStart = indices[i];
                uint fileEnd = indices[i + 1];

                DataBlock table = new();

                bs.Position = indexSize + fileStart;
                table.Read(bs);

                Elements.Add(table);
            }
            bs.Dispose();
        }

        private void WriteDatabase(string path)
        {
            using FileStream outStream = new FileStream(path, FileMode.Create);
            using BinaryStream bs = new BinaryStream(outStream, ByteConverter.Little);
            bs.WriteUInt32(ExpectedMagic);
            bs.WriteUInt32((uint)Elements.Count);
            bs.WriteUInt32(0); // Index size write at the end
            bs.WriteUInt32(7);

            long indicesOffset = bs.Position;
            bs.Position += (Elements.Count + 1) * sizeof(int);
            bs.Align(0x08, grow: true);

            long indexSize = bs.Position;

            long lastOffset = 0;
            for (int i = 0; i < Elements.Count; i++)
            {
                long elemOffset = bs.Position;
                Elements[i].Write(outStream);
                lastOffset = bs.Position;

                bs.Position = indicesOffset + (i * sizeof(int));
                bs.WriteUInt32((uint)(elemOffset - indexSize));

                bs.Position = lastOffset;
            }

            bs.Position = indicesOffset + (Elements.Count * sizeof(int));
            bs.WriteUInt32((uint)lastOffset);

            bs.Position = 0x08;
            bs.WriteUInt32((uint)indexSize);
        }

        public void Save(string outputDir, string suffix = "")
        {
            string suffixStr = string.IsNullOrEmpty(suffix) ? "" : $"_{suffix}";

            Directory.CreateDirectory(outputDir);
            WriteDatabase(Path.Combine(outputDir, $"paramdb{suffixStr}.db"));

            using var strDbStream = new FileStream(Path.Combine(outputDir, $"paramstr{suffixStr}.db"), FileMode.Create);
            StringDataBase.BytesPerCharacter = 1;
            StringDataBase.Write(strDbStream);

            using var uniStrDbStream = new FileStream(Path.Combine(outputDir, $"paramunistr{suffixStr}.db"), FileMode.Create);
            UnicodeStringDataBase.BytesPerCharacter = -1;
            UnicodeStringDataBase.Write(uniStrDbStream);

            // ID Table + ID String table
            using var idTableStream = new FileStream(Path.Combine(outputDir, $".id_db_idx{suffixStr}.db"), FileMode.Create);
            IDTable.Write(idTableStream);

            using var idStrDbStream = new FileStream(Path.Combine(outputDir, $".id_db_str{suffixStr}.db"), FileMode.Create);
            IDStringDataBase.BytesPerCharacter = 1;
            IDStringDataBase.Write(idStrDbStream);
        }

        public void ReadStringDatabase(string stringDatabaseName)
        {
            StringDataBase.Read(stringDatabaseName);
        }

        public void ReadUniStringDatabase(string uniStrDatabaseName)
        {
            UnicodeStringDataBase.Read(uniStrDatabaseName);
        }

        public void InitIDTables(string idDatabasePath, string idStringsDatabasePath)
        {
            IDTable.Read(idDatabasePath);

            IDStringDataBase.Read(idStringsDatabasePath);
        }

        public DataBlock GetFile(int index)
        {
            return Elements[index];
        }

        public int GetNumElements()
        {
            return Elements.Count;
        }

        public List<DataBlock> GetElements()
        {
            return Elements;
        }

        public Span<byte> GetBlockElement(int file, ulong hash)
        {
            DataBlock block = GetFile(file);
            long index = block.SearchEntry(hash);
            if (index == -1)
                return null;

            return block.Buffer.AsSpan((int)(index * block.ElementSize), block.ElementSize);
        }

        public long SearchElement(int file, ulong hash)
        {
            DataBlock fileBlock = GetFile(file);
            return fileBlock.SearchEntry(hash);
        }

        public string GetIDString(ulong hash)
        {
            if (hash == 0)
                return "NULL";

            long index = IDTable.GetStringIndex(hash);
            if (index == -1)
                return null;

            return IDStringDataBase.Strings[(int)index];
        }

        public string GetString(int index)
        {
            return StringDataBase.Strings[(int)index];
        }

        public string GetMultiByteString(int index)
        {
            return UnicodeStringDataBase.Strings[(int)index];
        }
    }

    public enum CarDatabaseFileType
    {
        BRAKE,
        BRAKECONTROLLER,
        STEER,
        CHASSIS,
        LIGHTWEIGHT,
        RACINGMODIFY,
        ENGINE,
        PORTPOLISH,
        ENGINEBALANCE,
        DISPLACEMENT,
        COMPUTER,
        NATUNE,
        TURBINEKIT,
        DRIVETRAIN,
        FLYWHEEL,
        CLUTCH,
        PROPELLERSHAFT,
        GEAR,
        SUSPENSION,
        INTERCOOLER,
        MUFFLER,
        LSD,
        TCSC,
        ASCC,
        WHEEL,
        TIRESIZE,
        TIREFORCEVOL,
        TIRECOMPOUND,
        FRONTTIRE,
        REARTIRE,
        CAR,
        ENEMY_CARS,
        EVENT,
        REGULATIONS,
        COURSE,
        ARCADE_CAR
    }
}
