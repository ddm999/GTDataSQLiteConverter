//#define PRINT_SQL_QUERIES
//#define DONT_RUN_SQL

using Microsoft.Data.Sqlite;
using Syroot.BinaryData;

namespace DBHelper
{
    public class DBTable
    {
        public const string HeaderDefinitionPath = "Headers";

        public string Name { get; set; }

        public long entryCount;
        public uint dataStart;

        public int RowLength { get; set; }
        public bool ForcedRowLength = false;

        public List<TableColumn> Columns = new();
        public List<TableRow> Rows = new();

    }

    public class DBUtils
    {
        public static List<TableRow> ReadRows(BinaryStream bs, long dataStart, long entryCount, List<TableColumn> columns, int rowLength, Func<ulong, object> stringHandler)
        {
            var rows = new List<TableRow>();
            for (var i = 0; i < entryCount; i++)
            {
                TableRow row = new TableRow();

                for (int j = 0; j < columns.Count; j++)
                {
                    TableColumn col = columns[j];
                    bs.Position = dataStart + (i * rowLength) + col.Offset;

                    if (col.Type == DBColumnType.String)
                    {
                        row.Cells.Add(stringHandler(bs.ReadUInt64()));
                    }
                    else if (col.Type == DBColumnType.Int64)
                    {
                        row.Cells.Add(bs.ReadUInt64());
                    }
                    else if (col.Type == DBColumnType.Int)
                    {
                        row.Cells.Add(bs.ReadUInt32());
                    }
                    else if (col.Type == DBColumnType.Float)
                    {
                        row.Cells.Add(bs.ReadSingle());
                    }
                    else if (col.Type == DBColumnType.Double)
                    {
                        row.Cells.Add(bs.ReadDouble());
                    }
                    else if (col.Type == DBColumnType.Byte)
                    {
                        row.Cells.Add(bs.ReadByte());
                    }
                    else if (col.Type == DBColumnType.Short)
                    {
                        row.Cells.Add(bs.ReadUInt16());
                    }
                }

                rows.Add(row);
            }

            return rows;
        }

        public static int TypeToSize(DBColumnType type)
        {
            // can't use a switch for types :(
            if (type == DBColumnType.String || type == DBColumnType.Int64 || type == DBColumnType.Double)
                return 8;
            else if (type == DBColumnType.Int || type == DBColumnType.Float)
                return 4;
            else if (type == DBColumnType.Short)
                return 2;
            else if (type == DBColumnType.Byte)
                return 1;

            return -1;
        }

        public static DBColumnType ColumnTypeToType(string str) =>
            str switch
            {
                "str" or "string" => DBColumnType.String,
                "int8" or "sbyte" => DBColumnType.Byte,
                "int16" or "short" or "2" => DBColumnType.Short,
                "int32" or "int" or "4" => DBColumnType.Int,
                // uint64 isn't supported by sqlite, so it's fine to read as int64
                "int64" or "uint64" or "long" or "ulong" or "8" => DBColumnType.Int64,
                "uint8" or "byte" or "1" => DBColumnType.Byte,
                "uint16" or "ushort" => DBColumnType.Short,
                "uint32" or "uint" => DBColumnType.Int,
                "float" => DBColumnType.Float,
                "double" => DBColumnType.Double,
                _ => DBColumnType.Unknown,
            };

        public static void ExportTableToSQLite(DBTable table, SqliteConnection connection)
        {
            //SQL: DROP TABLE IF EXISTS
            var command = connection.CreateCommand();
            command.CommandText = $"DROP TABLE IF EXISTS \"{table.Name}\";";

#if DONT_RUN_SQL
            Console.WriteLine($"Skipping DROP TABLE IF EXISTS for '{table.Name}'.");
            command.Cancel();
#else
            Console.WriteLine($"Running DROP TABLE IF EXISTS for '{table.Name}'.");
            command.ExecuteNonQuery();
#endif


            //SQL: CREATE TABLE
            string tableDefinition = $"CREATE TABLE \"{table.Name}\" (\n";
            foreach (TableColumn column in table.Columns)
            {
                tableDefinition += $"    \"{column.Name}\" {TypeToSQLiteTypeName(column.Type)},\n";
            }

            tableDefinition = tableDefinition.Remove(tableDefinition.Length - 2); // replace trailing comma
            tableDefinition += "\n";
            tableDefinition += ");";

#if PRINT_SQL_QUERIES
            Console.WriteLine(tableDefinition);
#endif
            command = connection.CreateCommand();
            command.CommandText = tableDefinition;
#if DONT_RUN_SQL
            Console.WriteLine($"Skipping CREATE TABLE for '{table.Name}'.");
            command.Cancel();
#else
            Console.WriteLine($"Running CREATE TABLE for '{table.Name}'.");
            command.ExecuteNonQuery();
#endif

            //SQL: INSERT INTO
            if (table.Rows.Count > 0)
            {
                string insertDefinition = $"INSERT INTO \"{table.Name}\" (";
                foreach (TableColumn header in table.Columns)
                {
                    insertDefinition += $"\"{header.Name}\", ";
                }
                insertDefinition = insertDefinition.Remove(insertDefinition.Length - 2); // replace trailing comma
                insertDefinition += ")\n" +
                                    "VALUES\n";

                for (int entryCounter = 0; entryCounter < table.Rows.Count; entryCounter++)
                {
                    insertDefinition += "    (";
                    var row = table.Rows[entryCounter];

                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        TableColumn column = table.Columns[i];

                        insertDefinition += column.Type switch
                        {
                            DBColumnType.String => $"'{((string)row.Cells[i]).Replace("'", "''")}', ",
                            DBColumnType.Int => $"{(uint)row.Cells[i]}, ",
                            DBColumnType.Float => $"{(float)row.Cells[i]}, ",
                            DBColumnType.Int64 => $"{(ulong)row.Cells[i]}, ",
                            DBColumnType.Short => $"{(ushort)row.Cells[i]}, ",
                            DBColumnType.Byte => $"{(int)row.Cells[i]}, ",
                            DBColumnType.Double => $"{(double)row.Cells[i]}, ",
                        };
                    }

                    insertDefinition = insertDefinition.Remove(insertDefinition.Length - 2); // replace trailing comma
                    insertDefinition += "),\n";

                    if (entryCounter % 100 == 99 && entryCounter < table.Rows.Count - 25)
                    {
                        insertDefinition = insertDefinition.Remove(insertDefinition.Length - 2); // replace trailing comma
                        insertDefinition += ";\n";

#if PRINT_SQL_QUERIES
                        Console.WriteLine(insertDefinition);
#endif

                        command = connection.CreateCommand();
                        command.CommandText = insertDefinition;
#if DONT_RUN_SQL
                    Console.WriteLine($"Skipping early ({(100.0f * entryCounter) / (1.0f * table.Rows.Count)}%) INSERT INTO for '{table.Name}'.");
                    command.Cancel();
#else
                        Console.WriteLine($"Running early ({(100.0f * entryCounter) / (1.0f * table.Rows.Count)}%) INSERT INTO for '{table.Name}'.");
                        command.ExecuteNonQuery();
#endif

                        insertDefinition = $"INSERT INTO \"{table.Name}\" (";
                        foreach (TableColumn column in table.Columns)
                        {
                            insertDefinition += $"\"{column.Name}\", ";
                        }
                        insertDefinition = insertDefinition.Remove(insertDefinition.Length - 2); // replace trailing comma
                        insertDefinition += ")\n" +
                                            "VALUES\n";
                    }
                }

                insertDefinition = insertDefinition.Remove(insertDefinition.Length - 2); // replace trailing comma
                insertDefinition += ";\n";

                /*
                var errorValue = ((stringDataOffset == 0 ? bs.Length : stringDataOffset) - bs.Position) / entryCount;
                if (errorValue > 0)
                {
                    Console.WriteLine($"Warning: Headers for table {table.Name} are incorrect.\n" +
                                $" Entries are {errorValue} byte(s) too large.");
                }
                else if (errorValue < 0)
                {
                    Console.WriteLine($"Warning: Headers for table {table.Name} are incorrect.\n" +
                                $" Entries are {Math.Abs(errorValue)} byte(s) too small.");
                }
                */

#if PRINT_SQL_QUERIES
                Console.WriteLine(insertDefinition);
#endif

                command = connection.CreateCommand();
                command.CommandText = insertDefinition;
#if DONT_RUN_SQL
                Console.WriteLine($"Skipping INSERT INTO for '{table.Name}'.");
                command.Cancel();
#else
                Console.WriteLine($"Running INSERT INTO for '{table.Name}'.");
                command.ExecuteNonQuery();
#endif
            }
            Console.WriteLine($"All done for '{table.Name}'.");
        }

        public static string? TypeToSQLiteTypeName(DBColumnType type)
        {
            // can't use a switch for types :(
            if (type == DBColumnType.String)
                return "TEXT";
            else if (type == DBColumnType.Byte || type == DBColumnType.Short || type == DBColumnType.Int || type == DBColumnType.Int64)
                return "INTEGER";
            else if (type == DBColumnType.Float || type == DBColumnType.Double)
                return "REAL";
            else
                return null;
        }
    }

    public class TableColumn
    {
        public string Name { get; set; }
        public DBColumnType Type { get; set; }
        public long Offset { get; set; }

        public override string ToString()
        {
            return $"{Name} ({Type} at {Offset:X8})";
        }
    }

    public class TableRow
    {
        public List<object> Cells { get; set; } = new List<object>();
    }

    public enum DBColumnType
    {
        Unknown,
        Byte,
        Short,
        Int,
        Float,
        Int64,
        Double,
        String
    }
}
