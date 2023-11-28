using Microsoft.Data.Sqlite;
using Syroot.BinaryData;

namespace GTDataSQLiteConverter
{
    public class CarDataBase
    {
        private readonly uint ExpectedMagic = 0x52415447;

        private List<DataBlock> _elements = new();
        private StringsDataBase _stringDb;
        private StringsDataBase _unicodeStringDb;

        private IDTable _idTable;
        private StringsDataBase _idStringTable;

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

            _elements = new List<DataBlock>((int)numFiles);
            for (int i = 0; i < numFiles; i++)
            {
                uint fileStart = indices[i];
                uint fileEnd = indices[i + 1];

                DataBlock table = new();

                bs.Position = indexSize + fileStart;
                table.Read(bs);

                _elements.Add(table);
            }
            bs.Dispose();
        }

        public void ReadStringDatabase(string stringDatabaseName)
        {
            _stringDb = new StringsDataBase();
            _stringDb.Read(stringDatabaseName);
        }

        public void ReadUniStringDatabase(string uniStrDatabaseName)
        {
            _unicodeStringDb = new StringsDataBase();
            _unicodeStringDb.Read(uniStrDatabaseName);
        }

        public void InitIDTables(string idDatabasePath, string idStringsDatabasePath)
        {
            _idTable = new IDTable();
            _idTable.Read(idDatabasePath);

            _idStringTable = new StringsDataBase();
            _idStringTable.Read(idStringsDatabasePath);
        }

        public DataBlock GetFile(int index)
        {
            return _elements[index];
        }

        public int GetNumElements()
        {
            return _elements.Count;
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

            long index = _idTable.GetStringIndex(hash);
            if (index == -1)
                return null;

            return _idStringTable.Strings[(int)index];
        }

        public string GetString(int index)
        {
            return _stringDb.Strings[(int)index];
        }

        public string GetMultiByteString(int index)
        {
            return _unicodeStringDb.Strings[(int)index];
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
