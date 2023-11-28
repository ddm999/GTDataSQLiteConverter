using Microsoft.Data.Sqlite;
using Syroot.BinaryData;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData.Memory;

namespace GTDataSQLiteConverter
{
    public class SQLiteExporter
    {
        private CarDataBase _database;

        public SQLiteExporter(CarDataBase database)
        {
            _database = database;
        }

        public void ExportTables(string sqliteDbFile)
        {
            using (var connection = new SqliteConnection($"Data Source={sqliteDbFile}"))
            {
                connection.Open();

                for (int i = 0; i < _database.GetNumElements(); i++)
                {
                    CarDatabaseFileType type = (CarDatabaseFileType)i;
                    var table = _database.GetFile(i);

                    string tableName = type.ToString();

                    string headersFile = TableMappingReader.GetHeadersFile(tableName);
                    if (string.IsNullOrEmpty(headersFile))
                    {
                        Console.WriteLine($"Skipped '{tableName}': unmapped.");
                        continue;
                    }

                    Console.WriteLine($"Reading '{tableName}'.");
                    var columnMappings = TableMappingReader.ReadColumnMappings(headersFile, out int readSize);
                    if (table.ElementSize != readSize)
                        Console.WriteLine($"WARNING: '{tableName}' non-matching mapped size");

                    var rows = ReadRows(table, columnMappings, 0);

                    ExportTableToSQLite(tableName, columnMappings, rows, connection);
                }

                connection.Close();
            }
        }

        private List<TableRow> ReadRows(DataBlock dataBlock, List<TableColumn> columns, int rowLength)
        {
            var rows = new List<TableRow>();
            for (var i = 0; i < dataBlock.NumOfElements; i++)
            {
                TableRow row = new TableRow();

                Span<byte> rowData = dataBlock.GetEntry(i);
                SpanReader sr = new SpanReader(rowData);

                for (int j = 0; j < columns.Count; j++)
                {
                    TableColumn col = columns[j];
                    sr.Position = (int)col.Offset;

                    switch (col.Type)
                    {
                        case DBColumnType.Id:
                            {
                                string str = _database.GetIDString(sr.ReadUInt64());
                                row.Cells.Add(str);
                            }
                            break;
                        case DBColumnType.String:
                            {
                                string str = _database.GetString(sr.ReadInt16());
                                row.Cells.Add(str);
                            }
                            break;
                        case DBColumnType.Unicode:
                            {
                                string str = _database.GetMultiByteString(sr.ReadInt16());
                                row.Cells.Add(str);
                            }
                            break;
                        case DBColumnType.Int64:
                            row.Cells.Add(sr.ReadUInt64());
                            break;
                        case DBColumnType.Int:
                            row.Cells.Add(sr.ReadUInt32());
                            break;
                        case DBColumnType.Float:
                            row.Cells.Add(sr.ReadSingle());
                            break;
                        case DBColumnType.Double:
                            row.Cells.Add(sr.ReadDouble());
                            break;
                        case DBColumnType.Byte:
                            row.Cells.Add(sr.ReadByte());
                            break;
                        case DBColumnType.Short:
                            row.Cells.Add(sr.ReadUInt16());
                            break;

                        default:
                            break;
                    }
                    
                }

                rows.Add(row);
            }

            return rows;
        }

        public static void ExportTableToSQLite(string name, List<TableColumn> columns, List<TableRow> rows, SqliteConnection connection)
        {
            //SQL: DROP TABLE IF EXISTS
            var command = connection.CreateCommand();
            command.CommandText = $"DROP TABLE IF EXISTS \"{name}\";";

#if DONT_RUN_SQL
            Console.WriteLine($"Skipping DROP TABLE IF EXISTS for '{name}'.");
            command.Cancel();
#else
            Console.WriteLine($"Running DROP TABLE IF EXISTS for '{name}'.");
            command.ExecuteNonQuery();
#endif


            //SQL: CREATE TABLE
            string tableDefinition = $"CREATE TABLE \"{name}\" (\n";
            foreach (TableColumn column in columns)
            {
                tableDefinition += $"    \"{column.Name}\" {DBUtils.TypeToSQLiteTypeName(column.Type)},\n";
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
            Console.WriteLine($"Skipping CREATE TABLE for '{name}'.");
            command.Cancel();
#else
            Console.WriteLine($"Running CREATE TABLE for '{name}'.");
            command.ExecuteNonQuery();
#endif

            //SQL: INSERT INTO
            if (rows.Count > 0)
            {
                string insertDefinition = $"INSERT INTO \"{name}\" (";
                foreach (TableColumn header in columns)
                {
                    insertDefinition += $"\"{header.Name}\", ";
                }
                insertDefinition = insertDefinition.Remove(insertDefinition.Length - 2); // replace trailing comma
                insertDefinition += ")\n" +
                                    "VALUES\n";

                for (int entryCounter = 0; entryCounter < rows.Count; entryCounter++)
                {
                    insertDefinition += "    (";
                    var row = rows[entryCounter];

                    for (int i = 0; i < columns.Count; i++)
                    {
                        TableColumn column = columns[i];

                        insertDefinition += column.Type switch
                        {
                            DBColumnType.String => $"'{((string)row.Cells[i]).Replace("'", "''")}', ",
                            DBColumnType.Unicode => $"'{((string)row.Cells[i]).Replace("'", "''")}', ",
                            DBColumnType.Id => $"'{((string)row.Cells[i]).Replace("'", "''")}', ",
                            DBColumnType.Int => $"{(uint)row.Cells[i]}, ",
                            DBColumnType.Float => $"{(float)row.Cells[i]}, ",
                            DBColumnType.Int64 => $"{(ulong)row.Cells[i]}, ",
                            DBColumnType.Short => $"{(ushort)row.Cells[i]}, ",
                            DBColumnType.Byte => $"{(byte)row.Cells[i]}, ",
                            DBColumnType.Double => $"{(double)row.Cells[i]}, ",
                        };
                    }

                    insertDefinition = insertDefinition.Remove(insertDefinition.Length - 2); // replace trailing comma
                    insertDefinition += "),\n";

                    if (entryCounter % 100 == 99 && entryCounter < rows.Count - 25)
                    {
                        insertDefinition = insertDefinition.Remove(insertDefinition.Length - 2); // replace trailing comma
                        insertDefinition += ";\n";

#if PRINT_SQL_QUERIES
                        Console.WriteLine(insertDefinition);
#endif

                        command = connection.CreateCommand();
                        command.CommandText = insertDefinition;
#if DONT_RUN_SQL
                    Console.WriteLine($"Skipping early ({(100.0f * entryCounter) / (1.0f * rows.Count)}%) INSERT INTO for '{name}'.");
                    command.Cancel();
#else
                        Console.WriteLine($"Running early ({(100.0f * entryCounter) / (1.0f * rows.Count)}%) INSERT INTO for '{name}'.");
                        command.ExecuteNonQuery();
#endif

                        insertDefinition = $"INSERT INTO \"{name}\" (";
                        foreach (TableColumn column in columns)
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
                    Console.WriteLine($"Warning: Headers for table {name} are incorrect.\n" +
                                $" Entries are {errorValue} byte(s) too large.");
                }
                else if (errorValue < 0)
                {
                    Console.WriteLine($"Warning: Headers for table {name} are incorrect.\n" +
                                $" Entries are {Math.Abs(errorValue)} byte(s) too small.");
                }
                */

#if PRINT_SQL_QUERIES
                Console.WriteLine(insertDefinition);
#endif

                command = connection.CreateCommand();
                command.CommandText = insertDefinition;
#if DONT_RUN_SQL
                Console.WriteLine($"Skipping INSERT INTO for '{name}'.");
                command.Cancel();
#else
                Console.WriteLine($"Running INSERT INTO for '{name}'.");
                command.ExecuteNonQuery();
#endif
            }
            Console.WriteLine($"All done for '{name}'.");
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
        Id,
        String,
        Unicode,
    }
}
