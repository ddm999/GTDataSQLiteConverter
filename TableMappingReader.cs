using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTDataSQLiteConverter.Entities;

namespace GTDataSQLiteConverter
{
    public class TableMappingReader
    {
        public static List<TableColumn> ReadColumnMappings(string tableName, out int readSize, string version_ident)
        {
            int offset = 0;
            List<TableColumn> columns = IterativeHeadersReader(tableName, ref offset, version_ident);

            readSize = offset;
            return columns;
        }

        public static string? GetHeadersFile(string tableName, bool checkSize = false)
        {
            string headersFilename = Path.Combine("Headers", Path.ChangeExtension(tableName, ".headers"));
            if (File.Exists(headersFilename))
            {
                if (checkSize)
                {
                    using var fs = new FileStream(headersFilename, FileMode.Open);
                    if (fs.Length > 0)
                    {
                        return headersFilename;
                    }
                }
                else
                {
                    return headersFilename;
                }
            }
            return null;
        }

        private static List<TableColumn> IterativeHeadersReader(string filename, ref int offset, string version_ident)
        {
            using var sr = new StreamReader(filename);

            List<TableColumn> columns = new();
            var dir = Path.GetDirectoryName(filename);
            var fn = Path.GetFileNameWithoutExtension(Path.GetFileName(filename));
            int lineNumber = 0;
            while (!sr.EndOfStream)
            {
                lineNumber++;
                var debugln = $"{fn}:{lineNumber}";

                var line = sr.ReadLine()?.Trim();

                // support comments & skip empty lines
                if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
                    continue;

                var split = line.Split("|");
                var id = split[0];
                string version_check = "";

                // Require version check to pass for data functions to run
                if (version_check == "" || version_check == version_ident)
                {
                    // === Data functions ===
                    if (id == "add_column")
                    {
                        if (split.Length < 3 || split.Length > 4)
                            Console.WriteLine($"Metadata error: {debugln} has malformed 'add_column' - expected 2 or 3 arguments (name, type, offset?), may break!");

                        string columnName = split[1];
                        string columnTypeStr = split[2];

                        DBColumnType columnType = DBUtils.ColumnTypeToType(columnTypeStr);
                        if (columnType == DBColumnType.Unknown)
                            Console.WriteLine($"Metadata error: {debugln} has malformed 'add_column' - type '{columnTypeStr}' is invalid\n" +
                                $"Valid types: str, int8, int16, int32/int, int64, uint8, uint16, uint32/uint, uint64, float, double");

                        var column = new TableColumn
                        {
                            Name = columnName,
                            Type = columnType
                        };

                        if (split.Length == 3)
                            column.Offset = offset;
                        else
                            column.Offset = Convert.ToInt64(split[3], 16);

                        offset += DBUtils.TypeToSize(columnType);

                        columns.Add(column);
                    }
                    else if (id == "padding")
                    {
                        if (split.Length != 2)
                            Console.WriteLine($"Metadata error: {debugln} has malformed 'padding' - expected 1 argument (length), may break!");

                        offset += Convert.ToInt32(split[1], 16);
                    }
                    else if (id == "include")
                    {
                        if (split.Length != 2)
                            Console.WriteLine($"Metadata error: {debugln} has malformed 'include' - expected 1 argument (filename), may break!");


                        var headersFilename = GetHeadersFile($"{split[1]}.headers");
                        if (headersFilename == null)
                        {
                            Console.WriteLine($"Metadata error: unknown include file '{split[1]}.headers' - may break!");
                            continue;
                        }

                        columns.AddRange(IterativeHeadersReader(headersFilename, ref offset, version_ident));
                    }
                }

                // === Version functions ===
                if (id == "supported_versions")
                {
                    if (split.Length != 2)
                        Console.WriteLine($"Metadata error: {debugln} has malformed 'supported_versions' - expected 1 argument (comma separated list of version identifiers), may break!");

                    string version_list = split[1];
                    string[] versions = version_list.Split(',');
                    Console.WriteLine($"SUPPORTED VERSION CHECK vl={version_list} vi={version_ident}");

                    if (!versions.Contains(version_ident))
                    {
                        Console.WriteLine($"Metadata error: {fn} headers do not support version {version_ident}. Skipping table.");
                        break;
                    }
                }
                else if (id == "set_version")
                {
                    if (split.Length != 2)
                        Console.WriteLine($"Metadata error: {debugln} has malformed 'set_version' - expected 1 argument (version identifier), may break!");

                    version_check = split[1];
                }
                else if (id == "reset_version")
                {
                    version_check = "";
                }
            }

            return columns;
        }
    }
}
