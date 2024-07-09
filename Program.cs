using CommandLine;

namespace GTDataSQLiteConverter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("[-- GT3DataSQLiteConverter by ddm, based on GT3DataSplitter by pez2k -- ]");

            Parser.Default.ParseArguments<ExportVerbs, ImportVerbs>(args)
                .WithParsed<ExportVerbs>(Export)
                .WithParsed<ImportVerbs>(Import)
                .WithNotParsed(HandleNotParsedArgs);
        }

        static void Export(ExportVerbs exportVerbs)
        {
            if (!File.Exists(exportVerbs.InputPath))
                throw new InvalidDataException("Input paramdb does not exist.");

            string dir = Path.GetDirectoryName(exportVerbs.InputPath);
            string fn = Path.GetFileName(exportVerbs.InputPath);

            string? suffix = exportVerbs.Suffix;
            if (suffix == null)
                suffix = "";
            else
                suffix = $"_{suffix}";

            string idxPath = Path.Combine(dir, $".id_db_idx{suffix}.db");
            string idstrPath = Path.Combine(dir, $".id_db_str{suffix}.db");
            string pmstrPath = Path.Combine(dir, $"paramstr{suffix}.db");
            string unistrPath = Path.Combine(dir, $"paramunistr{suffix}.db");
            string colPath = Path.Combine(dir, "carcolor.sdb");

            string? version = exportVerbs.Version;
            version ??= exportVerbs.Suffix;
            version ??= "jp";
            version = version.ToLower();

            //var coltable = new StringsDataBase();
            //coltable.Read(colPath);

            var database = new CarDataBase();
            database.InitSubDatabases(exportVerbs.InputPath, pmstrPath, unistrPath, version);
            database.InitIDTables(idxPath, idstrPath);

            string outPath;
            if (exportVerbs.OutputPath is not null)
                outPath = exportVerbs.OutputPath;
            else 
                outPath = Path.ChangeExtension(exportVerbs.InputPath, ".sqlite");
            
            var exporter = new SQLiteExporter(database);
            exporter.ExportTables(outPath);
        }

        static void Import(ImportVerbs importVerbs)
        {
            var importer = new SQLiteImporter(importVerbs.InputPath);

            importVerbs.OutputPath ??= Path.GetDirectoryName(importVerbs.InputPath);

            string? version = importVerbs.Version;
            version ??= importVerbs.Suffix;
            version ??= "jp";
            version = version.ToLower();

            importer.Import(importVerbs.OutputPath, importVerbs.Suffix, version);
        }

        public static void HandleNotParsedArgs(IEnumerable<Error> errors) {}
    }

    [Verb("export", HelpText = "Export a GT3 paramdb to an SQLite database.")]
    public class ExportVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input GT3 paramdb file. Relevant data files must be in the same location.")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output SQLite database file. Default is based on the paramdb.")]
        public string? OutputPath { get; set; }

        [Option('s', "suffix", HelpText = "Input suffix that is appended to find data files, i.e. `eu` = `.id_db_idx_eu.db`, `.id_db_str_eu.db`, `paramstr_eu`, `paramunistr_eu`. Default is based on the paramdb.")]
        public string? Suffix { get; set; }

        [Option('I', "ident", HelpText = "Version identifier. Used to distinguish between different formats for the same tables. Default is based on suffix.")]
        public string? Version { get; set; }
    }

    [Verb("import", HelpText = "Import a SQLite database into a GT3 paramdb.")]
    public class ImportVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input GT3 sqlite file.")]
        public string InputPath { get; set; }

        [Option('o', "output", Required = false, HelpText = "Output SQLite database file. Default is based on the sqlite.")]
        public string? OutputPath { get; set; }

        [Option('s', "suffix", HelpText = "Suffix to append to the output files, i.e. `eu` = `paramdb_eu.db`. Default is no suffix (GT3 JP).")]
        public string Suffix { get; set; }

        [Option('I', "ident", HelpText = "Version identifier. Used to distinguish between different formats for the same tables. Default is based on suffix.")]
        public string? Version { get; set; }
    }
}