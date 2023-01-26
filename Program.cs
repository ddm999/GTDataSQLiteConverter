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

            string strPath;
            if (exportVerbs.StringTablePath != null) {
                strPath = exportVerbs.StringTablePath;
            } else {
                var dir = Path.GetDirectoryName(exportVerbs.InputPath);
                var fn = Path.GetFileName(exportVerbs.InputPath);

                if (fn.Contains("_eu")) {
                    strPath = Path.Combine(dir, ".id_db_str_eu.db");
                } else if (fn.Contains("_us")) {
                    strPath = Path.Combine(dir, ".id_db_str_us.db");
                } else { 
                    strPath = Path.Combine(dir, ".id_db_str.db");
                }
            }
            
            string idxPath;
            if (exportVerbs.IDTablePath != null) {
                idxPath = exportVerbs.IDTablePath;
            } else {
                var dir = Path.GetDirectoryName(exportVerbs.InputPath);
                var fn = Path.GetFileName(exportVerbs.InputPath);

                if (fn.Contains("_eu")) {
                    idxPath = Path.Combine(dir, ".id_db_idx_eu.db");
                } else if (fn.Contains("_us")) {
                    idxPath = Path.Combine(dir, ".id_db_idx_us.db");
                } else {
                    idxPath = Path.Combine(dir, ".id_db_idx.db");
                }
            }

            var idtable = new IDTable();
            idtable.Read(idxPath, strPath);

            var database = new ParamDB();
            database.Read(exportVerbs.InputPath, ref idtable);

            string outPath;
            if (exportVerbs.OutputPath != null) {
                outPath = exportVerbs.OutputPath;
            } else {
                outPath = Path.ChangeExtension(exportVerbs.InputPath, ".sqlite");
            }

            database.ExportTables(outPath);
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

        [Option("str", HelpText = "Input GT3 id_db_str file. Default is based on the paramdb.")]
        public string StringTablePath { get; set; }

        [Option("idx", HelpText = "Input GT3 id_db_idx file. Default is based on the paramdb.")]
        public string IDTablePath { get; set; }
    }
}