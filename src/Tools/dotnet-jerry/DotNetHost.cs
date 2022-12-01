using Jerrycurl.Reflection;
using Jerrycurl.Tools.DotNet.Cli.Commands;
using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace Jerrycurl.Tools.DotNet.Cli
{
    public class DotNetHost
    {
        internal static Option<bool> VerboseOption { get; } = new Option<bool>("--verbose", "Show verbose output.");

        public async static Task<int> Main(string[] args)
        {
            WriteHeader();

#if DEBUG
            //Environment.CurrentDirectory = "C:\\Users\\thomas\\Desktop\\testx";

            //args = new[] { "orm", "sync", "--flags", "useNullables" };
            args = new[] { "orm", "new", "-v", "sqlserver", "-c", "server=.;database=gerstl_120922;trusted_connection=true;encrypt=false", "-i", @"c:\users\thomas\desktop\Database.orm" };
            //args = new[] { "orm", "sync", "-f", @"C:\Users\thomas\Desktop\Database.orm", "--verbose" };
            //args = new[] { "orm", "transform", "-f", @"c:\users\thomas\desktop\Database.orm" };
            //args = new[] { "orm", "run", "-f", @"c:\users\thomas\desktop\Database.orm", "--snippet", "test" };
            //args = new[] { "orm", "new", "-v", "sqlserver", "-c", "server=.;database=realescort_live;trusted_connection=true;encrypt=false" };
#endif
            RootCommand rootCommand = new RootCommand();

            rootCommand.AddGlobalOption(VerboseOption);

            new OrmCommandBuilder().Build(rootCommand);
            new RazorCommandBuilder().Build(rootCommand);

            return await rootCommand.InvokeAsync(args);
        }

        public static void WriteHeader()
        {
            NuGetVersion version = typeof(DotNetHost).Assembly.GetNuGetPackageVersion();

            WriteLine();

            string logo = @"      _       _              _        _                      
   __| | ___ | |_ _ __   ___| |_     (_) ___ _ __ _ __ _   _ 
  / _` |/ _ \| __| '_ \ / _ \ __|____| |/ _ \ '__| '__| | | |
 | (_| | (_) | |_| | | |  __/ ||_____| |  __/ |  | |  | |_| |
  \__,_|\___/ \__|_| |_|\___|\__|   _/ |\___|_|  |_|   \__, |
                                   |__/                |___/ ";
            string versionText = version.CommitHash != null ? $"v{version.PublicVersion} ({version.CommitHash})" : $"v{version.PublicVersion}";

            WriteLine(logo);
            WriteLine(versionText.PadLeft((logo.Length + versionText.Length) / 2));
            WriteLine();
        }

        public static void WriteLine() => Console.WriteLine();
        public static void WriteLine(string message, ConsoleColor? color = null)
        {
            if (color != null)
                Console.ForegroundColor = color.Value;

            Console.WriteLine(message);
            Console.ResetColor();
        }
    }
}
