using Jerrycurl.Reflection;
using Jerrycurl.Tools.Diagnostics;
using Jerrycurl.Tools.DotNet.Cli.Commands;
using Jerrycurl.Tools.Orm;
using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jerrycurl.Tools.DotNet.Cli;

public class DotNetHost
{
    internal static Option<bool> VerboseOption { get; } = new Option<bool>("--verbose") { Description = "Show verbose output.", Recursive = true };
    internal static Option<bool> LogoOption { get; } = new Option<bool>("--logo") { Description = "Show fancy logo before running.", Recursive = true };
    internal static Option<string> DebugOption { get; } = new Option<string>("--debug") { Description = "Output exception information to a machine-readable file.", Recursive = true, Hidden = true };

    public async static Task<int> Main(string[] args)
    {
#if DEBUG

        //Environment.CurrentDirectory = "C:\\Users\\thomas\\Desktop\\testx";

        //args = new[] { "orm", "diff", @"c:\users\thomas\desktop\Database.orm" };
        //args = new[] { "orm", "sync", "--flags", "useNullables" };
        //args = new[] { "orm", "new", "-v", "sqlserver", "-c", "server=.;database=gerstl_120922;trusted_connection=true;encrypt=false", "-i", @"c:\users\thomas\desktop\Database.orm", "--debug", @"c:\users\thomas\desktop\Database.log" };
        //args = new[] { "orm", "sync", "--vendor", "sqlserver", "-c", "server=.;database=gerstl_120922;trusted_connection=true;encrypt=false", "-ns", "Kaffezars", "--verbose", "--logo" };
        //args = new[] { "orm", "transform", "-f", @"c:\users\thomas\desktop\Database.orm" };
        //args = new[] { "orm", "run", "-f", @"c:\users\thomas\desktop\Database.orm", "--snippet", "test" };
        //args = new[] { "orm", "new", "-v", "sqlserver", "-c", "server=.;database=realescort_live;trusted_connection=true;encrypt=false" };



#endif
        RootCommand rootCommand = new RootCommand("Jerrycurl command-line interface.");

        rootCommand.Options.Add(VerboseOption);
        rootCommand.Options.Add(DebugOption);
        rootCommand.Options.Add(LogoOption);

        new OrmCommandBuilder().Build(rootCommand);
        new RazorCommandBuilder().Build(rootCommand);

        ParseResult result = rootCommand.Parse(args);

        if (result.GetValue(LogoOption))
            WriteLogo();

        try
        {
            return await result.InvokeAsync(new InvocationConfiguration() { EnableDefaultExceptionHandler = false });
        }
        catch (Exception ex)
        {
            return await HandleExceptionAsync(ex, result);
        }
    }

    private static async Task<int> HandleExceptionAsync(Exception ex, ParseResult result)
    {
        string debugPath = result.GetValue(DebugOption);
        bool verbose = result.GetValue(VerboseOption);

        if (debugPath != null)
            await WriteDebugInfoAsync(debugPath, ex);

        if (verbose)
            ErrorLine(ex.ToString());
        else
            ErrorLine(ex.Message);

        return ex is ToolException tex ? tex.ExitCode : -1;
    }

    private static async Task WriteDebugInfoAsync(string path, Exception ex)
    {
        DebugModel model = new DebugModel()
        {
            Message = ex.Message,
            Type = ex switch
            {
                OrmToolException oex => oex.Type,
                _ => null,
            },
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
    public static void WriteLogo()
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

    public static void Write(string message, ConsoleColor? color = null)
    {
        if (color != null)
            Console.ForegroundColor = color.Value;

        Console.Write(message);
        Console.ResetColor();
    }

    public static void ErrorLine(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine(message);
        Console.ResetColor();
    }
}
