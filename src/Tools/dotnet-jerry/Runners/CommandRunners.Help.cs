using Jerrycurl.Tools.DotNet.Cli.Commands;

namespace Jerrycurl.Tools.DotNet.Cli.Runners
{
    internal static partial class CommandRunners
    {
        public static void Help(RunnerArgs args)
        {
            if (args.Options.Default.Length == 0)
                HelpForInvalid(args);
            else if (args.Options.Default.Length == 1 || args.Options.Default[1] == "help")
            {
                DotNetJerryHost.WriteHeader();

                DotNetJerryHost.WriteLine("Usage: jerry [command] [options] [@file]");
                DotNetJerryHost.WriteLine();
                DotNetJerryHost.WriteLine("Execute a command with the specified options. Use @file[.cli] to expand arguments");
                DotNetJerryHost.WriteLine("from an input file.");
                DotNetJerryHost.WriteLine();
                DotNetJerryHost.WriteLine("Commands:");
                DotNetJerryHost.WriteLine("  scaffold                    Generate a C# object model from an existing database.");
                DotNetJerryHost.WriteLine("  transpile                   Transpile a project of .cssql files into .cs files.");
                DotNetJerryHost.WriteLine("  info                        Show information about a database connector.");
                DotNetJerryHost.WriteLine("  args                        Show all arguments. Useful for debugging @file inputs.");
                DotNetJerryHost.WriteLine("  help [command]              Show help information about the commands above.");
                DotNetJerryHost.WriteLine();
                DotNetJerryHost.WriteLine("Examples:");
                DotNetJerryHost.WriteLine("  Generate a C# model with arguments in a local 'mydb.cli' file:");
                DotNetJerryHost.WriteLine("  > jerry scaffold @mydb");
                DotNetJerryHost.WriteLine();
                DotNetJerryHost.WriteLine("  Transpile .cssql files from directories 'Queries' and 'Commands':");
                DotNetJerryHost.WriteLine("  > jerry transpile -d Queries -d Commands");
                DotNetJerryHost.WriteLine();
            }
            else
            {
                switch (args.Options.Default[1])
                {
                    case "scaffold":
                    case "sf":
                        HelpForScaffold();
                        break;
                    case "transpile":
                    case "tp":
                        HelpForTranspile();
                        break;
                    case "run":
                        HelpForRun();
                        break;
                    case "info":
                        HelpForInfo();
                        break;
                    default:
                        throw new RunnerException($"Invalid command '{args.Options.Default[1]}'.");
                }
            }
        }

        private static void HelpForScaffold()
        {
            DotNetJerryHost.WriteHeader();

            DotNetJerryHost.WriteLine("Usage: jerry scaffold [options]");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("Generate a C# object model from an existing database.");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("Options:");
            DotNetJerryHost.WriteLine("  -v,  --vendor <moniker>     Type of database to connect to: 'sqlserver', 'sqlite',");
            DotNetJerryHost.WriteLine("                                  'oracle', 'postgres' or 'mysql'.");
            DotNetJerryHost.WriteLine("  -c,  --connection <cs>      Connection string used to connect.");
            DotNetJerryHost.WriteLine("  -ns, --namespace <ns>       Namespace to place generated classes in.");
            DotNetJerryHost.WriteLine("  -o,  --output <file>        File or directory to generate .cs files into. Defaults");
            DotNetJerryHost.WriteLine("                                  to Database.cs.");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("Examples:");
            DotNetJerryHost.WriteLine("  Generate model for a local SQL Server database into 'Database.cs':");
            DotNetJerryHost.WriteLine("  > jerry scaffold -v sqlserver -c \"DATABASE=blogdb\" -ns BlogDb.Data");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("  Generate model into the 'Database' directory using one file per table:");
            DotNetJerryHost.WriteLine("  > jerry scaffold [...] -o Database");
            DotNetJerryHost.WriteLine();
        }

        private static void HelpForTranspile()
        {
            DotNetJerryHost.WriteHeader();

            DotNetJerryHost.WriteLine("Usage: jerry transpile [options]");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("Transpile a project of .cssql files into .cs files.");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("Options:");
            DotNetJerryHost.WriteLine("  -p,  --project         Project directory to resolve relative paths and namespaces from.");
            DotNetJerryHost.WriteLine("                              Defaults to the current directory.");
            DotNetJerryHost.WriteLine("  -f,  --file            Add a file to the project.");
            DotNetJerryHost.WriteLine("  -d,  --directory       Add all .cssql files to the project from a specified directory.");
            DotNetJerryHost.WriteLine("  -ns, --namespace       Root namespace for the project.");
            DotNetJerryHost.WriteLine("  -i,  --import          Add a namespace import.");
            DotNetJerryHost.WriteLine("  -o,  --output          Output directory for .cs files. Defaults to 'obj\\Jerrycurl'");
            DotNetJerryHost.WriteLine("  --no-clean             Do not clean the output directory before writing new files.");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("Examples:");
            DotNetJerryHost.WriteLine("  Transpile all .cssql files from the current directory with the specified root namespace:");
            DotNetJerryHost.WriteLine("  > jerry transpile -d . -ns BlogDb.Data");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("  Transpile a few .cssql files and import 'BlogDb.Database' namespace:");
            DotNetJerryHost.WriteLine("  > jerry transpile -f Query1.cssql Query2.cssql -i BlogDb.Database");
            DotNetJerryHost.WriteLine();
        }

        private static void HelpForInfo()
        {
            DotNetJerryHost.WriteHeader();

            DotNetJerryHost.WriteLine("Usage: jerry info [options]");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("Show information about a specific database connector.");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("Options:");
            DotNetJerryHost.WriteLine("  -v,  --vendor <moniker>     Type of database to connect to: 'sqlserver', 'sqlite',");
            DotNetJerryHost.WriteLine("                                  'oracle', 'postgres' or 'mysql'.");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("Examples:");
            DotNetJerryHost.WriteLine("  Show information about the Microsoft SQL Server connector:");
            DotNetJerryHost.WriteLine("  > jerry info -v sqlserver");
            DotNetJerryHost.WriteLine();
        }

        private static void HelpForRun()
        {
            DotNetJerryHost.WriteHeader();

            DotNetJerryHost.WriteLine("Usage: jerry run [options]");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("Run SQL statements against a database.");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("Options:");
            DotNetJerryHost.WriteLine("  -v, --vendor <moniker>      Type of database to connect to: 'sqlserver', 'sqlite',");
            DotNetJerryHost.WriteLine("                                  'oracle', 'postgres' or 'mysql'.");
            DotNetJerryHost.WriteLine("  -s, --sql <statements>      SQL string to execute on the server.");
            DotNetJerryHost.WriteLine("  -f, --file <file>           SQL file to execute. Expands lines from @-prefixed paths.");
            DotNetJerryHost.WriteLine("  --raw <file>                SQL file to execute. Does not expand lines.");
            DotNetJerryHost.WriteLine();
            DotNetJerryHost.WriteLine("Examples:");
            DotNetJerryHost.WriteLine("  Run a DELETE statement using config from a local 'mydb.cli' file:");
            DotNetJerryHost.WriteLine("  > jerry run @mydb --sql \"DELETE FROM [BlogPost]\"");
            DotNetJerryHost.WriteLine();
        }

        public static void HelpForInvalid(RunnerArgs args)
        {
            DotNetJerryHost.WriteHeader();
            DotNetJerryHost.WriteLine("Usage: jerry [command] [options].");
            DotNetJerryHost.WriteLine("Use 'jerry help' to show commands and options.");
            DotNetJerryHost.WriteLine();

            if (string.IsNullOrEmpty(args.Command))
                throw new RunnerException("No command specified.");
            else
                throw new RunnerException($"Invalid command '{args.Command}'.");
        }
    }
}
