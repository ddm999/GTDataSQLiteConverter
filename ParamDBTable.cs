using Syroot.BinaryData;
using DBHelper;

namespace GTDataSQLiteConverter
{
    enum ParamDBStringType
    {
        ID = 0,
        String,
        Unicode,
        Color
    }

    internal class ParamDBTable : DBTable
    {
        private readonly uint ExpectedMagic = 0x54445447;

        private IDTable IDs = new();
        private StringTable Strings = new();
        private StringTable UnicodeStrings = new();
        private StringTable ColorStrings = new();
        public void Read(string name, uint start, uint length, ref BinaryStream bs, ref IDTable ids,
                         ref StringTable paramstr, ref StringTable unistr, ref StringTable colorstr)
        {
            Name = name;
            IDs = ids;
            Strings = paramstr;
            UnicodeStrings = unistr;
            ColorStrings = colorstr;

            bs.Seek(start, SeekOrigin.Begin);

            var headersFilename = GetHeadersFile(Name);

            if (headersFilename != null)
            {
                ReadColumnMappings(headersFilename);
            }

            bs.Seek(start, SeekOrigin.Begin);

            var magic = bs.ReadUInt32();
            if (magic != ExpectedMagic)
            {
                throw new InvalidDataException("DB data is not a GTDT table.");
            }

            bs.Seek(0x4);
            ushort structCount = bs.ReadUInt16();
            ushort structSize = bs.ReadUInt16();
            if (structSize != RowLength)
                Console.WriteLine($"Warning: GTDT table row length ({structSize}) != Header row length {RowLength}, may break!");
            
            uint tableSize = bs.ReadUInt32();
            if (tableSize != length)
                Console.WriteLine($"Warning: GTDT table length ({tableSize}) != GTAR section length {length}, may break!");

            List<Func<ulong, object>> stringHandlers = new()
            {
                IDStringDataHandler,
                StringDataHandler,
                UniStringDataHandler,
                ColStringDataHandler
            };
            Rows = DBUtils.ReadRows(bs, bs.Position, structCount, Columns, structSize, stringHandlers);
        }

        public object IDStringDataHandler(ulong id)
        {
            if (id == 0)
                return "NULL";

            var val = IDs.Get(id);
            return val != null ? val : $"unknown idstring! {id}";
        }

        public object StringDataHandler(ulong id)
        {
            if (id == 0)
                return "NULL";

            var val = Strings.Get((ushort)id);
            return val != null ? val : $"unknown string! {id}";
        }

        public object UniStringDataHandler(ulong id)
        {
            if (id == 0)
                return "NULL";

            var val = UnicodeStrings.Get((ushort)id);
            return val != null ? val : $"unknown string! {id}";
        }

        public object ColStringDataHandler(ulong id)
        {
            if (id == 0)
                return "NULL";

            var val = ColorStrings.Get((ushort)id);
            return val != null ? val : $"unknown string! {id}";
        }

        private void ReadColumnMappings(string tableName)
        {
            int offset = 0;
            Columns = IterativeHeadersReader(tableName, ref offset);
            RowLength = offset;
        }

        private List<TableColumn> IterativeHeadersReader(string filename, ref int offset)
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
                    
                    columns.AddRange(IterativeHeadersReader(headersFilename, ref offset));
                }
            }

            return columns;
        }

        public static string? GetHeadersFile(string tableName, bool checkSize=false)
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

        public override string ToString()
        {
            return $"{Name}";
        }
    }
}
