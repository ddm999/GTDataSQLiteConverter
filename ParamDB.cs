using Microsoft.Data.Sqlite;
using Syroot.BinaryData;
using DBHelper;

namespace GTDataSQLiteConverter
{
    internal class ParamDB
    {
        private readonly uint ExpectedMagic = 0x52415447;
        private readonly string[] tablenames = {
            "BRAKE",
            "BRAKECONTROLLER",
            "STEER",
            "CHASSIS",
            "LIGHTWEIGHT",
            "RACINGMODIFY",
            "ENGINE",
            "PORTPOLISH",
            "ENGINEBALANCE",
            "DISPLACEMENT",
            "COMPUTER",
            "NATUNE",
            "TURBINEKIT",
            "DRIVETRAIN",
            "FLYWHEEL",
            "CLUTCH",
            "PROPELLERSHAFT",
            "GEAR",
            "SUSPENSION",
            "INTERCOOLER",
            "MUFFLER",
            "LSD",
            "TCSC",
            "ASCC",
            "WHEEL",
            "TIRESIZE",
            "TIREFORCEVOL",
            "TIRECOMPOUND",
            "FRONTTIRE",
            "REARTIRE",
            "CAR",
            "ENEMY_CARS",
            "EVENT",
            "REGULATIONS",
            "COURSE",
            "ARCADE_CAR"
        };

        private List<ParamDBTable> tables = new();
        public void Read(string filename, ref IDTable ids)
        {
            using var fs = new FileStream(filename, FileMode.Open);
            var bs = new BinaryStream(fs);

            var magic = bs.ReadUInt32();
            if (magic != ExpectedMagic)
            {
                throw new InvalidDataException("Input paramdb is not a GTAR archive.");
            }

            uint tableCount = bs.ReadUInt32();
            uint dataStart = bs.ReadUInt32();

            tables = new List<ParamDBTable>((int)tableCount);
            for (int i = 0; i < tableCount; i++)
            {
                bs.Seek(0x10+(i*4), SeekOrigin.Begin);
                uint tableStart = bs.ReadUInt32();
                uint tableEnd = bs.ReadUInt32();

                ParamDBTable table = new();
                string tablename = tablenames[i];
                if (ParamDBTable.GetHeadersFile(tablename, true) is null)
                {
                    Console.WriteLine($"Skipped '{tablename}': unmapped.");
                    continue;
                }

                Console.WriteLine($"Reading '{tablename}'.");
                table.Read(tablename, dataStart+tableStart, tableEnd-tableStart, ref ids, ref bs);
                tables.Add(table);
            }
            bs.Dispose();
        }

        public void ExportTables(string sqliteDbFile)
        {
            using (var connection = new SqliteConnection($"Data Source={sqliteDbFile}"))
            {
                connection.Open();

                foreach (var table in tables)
                {
                    DBUtils.ExportTableToSQLite(table, connection);
                }

                connection.Close();
            }
        }
    }
}
