using CommandLine;

namespace GTDataSQLiteConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[-- GT3DataSQLiteConverter by ddm, based on GT3DataSplitter by pez2k -- ]");

            Parser.Default.ParseArguments<ExportVerbs>(args)
                .WithParsed<ExportVerbs>(Export)
                .WithNotParsed(HandleNotParsedArgs);
        }

        static void Export(ExportVerbs exportVerbs)
        {
            if (!File.Exists(exportVerbs.InputPath))
                throw new InvalidDataException("Input paramdb does not exist.");

            string dir = Path.GetDirectoryName(exportVerbs.InputPath);
            string fn = Path.GetFileName(exportVerbs.InputPath);

            string? idxPath = exportVerbs.IDXTablePath;
            string? idstrPath = exportVerbs.IDStrTablePath;
            string? pmstrPath = exportVerbs.ParamStrTablePath;
            string? unistrPath = exportVerbs.UniStrTablePath;
            string? colPath = exportVerbs.ColorTablePath ?? Path.Combine(dir, "carcolor.sdb");
            if (fn.Contains("_eu"))
            {
                idxPath = idxPath ?? Path.Combine(dir, ".id_db_idx_eu.db");
                idstrPath = idstrPath ?? Path.Combine(dir, ".id_db_str_eu.db");
                pmstrPath = pmstrPath ?? Path.Combine(dir, "paramstr_eu.db");
                unistrPath = unistrPath ?? Path.Combine(dir, "paramunistr_eu.db");
            }
            else if (fn.Contains("_us"))
            {
                idxPath = idxPath ?? Path.Combine(dir, ".id_db_idx_us.db");
                idstrPath = idstrPath ?? Path.Combine(dir, ".id_db_str_us.db");
                pmstrPath = pmstrPath ?? Path.Combine(dir, "paramstr_us.db");
                unistrPath = unistrPath ?? Path.Combine(dir, "paramunistr_us.db");
            }
            else
            {
                idxPath = idxPath ?? Path.Combine(dir, ".id_db_idx.db");
                idstrPath = idstrPath ?? Path.Combine(dir, ".id_db_str.db");
                pmstrPath = pmstrPath ?? Path.Combine(dir, "paramstr.db");
                unistrPath = unistrPath ?? Path.Combine(dir, "paramunistr.db");
            }

            var coltable = new StringsDataBase();
            coltable.Read(colPath);

            var database = new CarDataBase();
            database.InitSubDatabases(exportVerbs.InputPath, pmstrPath, unistrPath);
            database.InitIDTables(idxPath, idstrPath);

            string outPath;
            if (exportVerbs.OutputPath is not null)
                outPath = exportVerbs.OutputPath;
            else 
                outPath = Path.ChangeExtension(exportVerbs.InputPath, ".sqlite");
            
            var exporter = new SQLiteExporter(database);
            exporter.ExportTables(outPath);
        }

        public static void HandleNotParsedArgs(IEnumerable<Error> errors) {}
    }

    [Verb("export", HelpText = "Export a GT3 paramdb to an SQLite database.")]
    public class ExportVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input GT3 paramdb file.")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output SQLite database file. Default is based on the paramdb.")]
        public string? OutputPath { get; set; }

        [Option("idx", HelpText = "Input GT3 id_db_idx file. Default is based on the paramdb.")]
        public string? IDXTablePath { get; set; }

        [Option("istr", HelpText = "Input GT3 id_db_str file. Default is based on the paramdb.")]
        public string? IDStrTablePath { get; set; }

        [Option("pstr", HelpText = "Input GT3 paramstr file. Default is based on the paramdb.")]
        public string? ParamStrTablePath { get; set; }

        [Option("ustr", HelpText = "Input GT3 paramunistr file. Default is based on the paramdb.")]
        public string? UniStrTablePath { get; set; }

        [Option("cstr", HelpText = "Input GT3 carcolor sdb file. Default is based on the paramdb.")]
        public string? ColorTablePath { get; set; }
    }
}