using Jerrycurl.Reflection;
using Jerrycurl.Tools.Diagnostics;
using Jerrycurl.Tools.DotNet.Cli.Commands;
using Jerrycurl.Tools.Orm;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jerrycurl.Tools.DotNet.Cli
{
    public class DotNetHost
    {
        internal static Option<bool> VerboseOption { get; } = new Option<bool>("--verbose", "Show verbose output.");
        internal static Option<string> DebugOption { get; } = new Option<string>("--debug", "Output exception information to a machine-readable file.") { IsHidden = true };

        public async static Task<int> Main(string[] args)
        {

            WriteHeader();

#if DEBUG
            
            //Environment.CurrentDirectory = "C:\\Users\\thomas\\Desktop\\testx";

            //args = new[] { "orm", "sync", "--flags", "useNullables" };
            //args = new[] { "orm", "new", "-v", "sqlserver", "-c", "server=.;database=gerstl_120922;trusted_connection=true;encrypt=false", "-i", @"c:\users\thomas\desktop\Database.orm", "--debug", @"c:\users\thomas\desktop\Database.log" };
            args = new[] { "orm", "sync", "-i", @"C:\Users\thomas\Desktop\Database.orm", "--debug", @"c:\users\thomas\desktop\Database.log" };
            //args = new[] { "orm", "transform", "-f", @"c:\users\thomas\desktop\Database.orm" };
            //args = new[] { "orm", "run", "-f", @"c:\users\thomas\desktop\Database.orm", "--snippet", "test" };
            //args = new[] { "orm", "new", "-v", "sqlserver", "-c", "server=.;database=realescort_live;trusted_connection=true;encrypt=false" };



#endif
            RootCommand rootCommand = new RootCommand()
            {
                Name = "jerry",
            };

            CommandLineBuilder b = new CommandLineBuilder(rootCommand);

            b.UseDefaults();
            b.UseExceptionHandler(async (ex, ctx) =>
            {
                string debugPath = ctx.GetValue(DebugOption);
                bool verbose = ctx.GetValue(VerboseOption);

                if (debugPath != null)
                    await WriteDebugInfoAsync(debugPath, ex);

                if (verbose)
                    WriteLine(ex.ToString(), ConsoleColor.Red);
                else
                    WriteLine(ex.Message, ConsoleColor.Red);
            });

            rootCommand.AddGlobalOption(VerboseOption);
            rootCommand.AddGlobalOption(DebugOption);

            new OrmCommandBuilder().Build(rootCommand);
            new RazorCommandBuilder().Build(rootCommand);

            Parser parser = b.Build();

            return await parser.InvokeAsync(args);
        }

        private static async Task WriteDebugInfoAsync(string path, Exception ex)
        {
            DebugModel model = new DebugModel()
            {
                Message = ex.Message,
                Log = ex switch
                {
                    OrmToolException oex => oex.Log,
                    _ => null,
                }
            };

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            using FileStream stream = File.OpenWrite(path);

            await JsonSerializer.SerializeAsync(stream, model, options);
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
                                   |__/                |___/ "
;
            string versionText = version.CommitHash != null ? $"v{version.PublicVersion} ({version.CommitHash})" : $"v{version.PublicVersion}";

            WriteLine(logo);
            WriteLine(versionText.PadLeft((60 + versionText.Length) / 2));
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
