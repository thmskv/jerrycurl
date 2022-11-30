﻿using Jerrycurl.Reflection;
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

            //Environment.CurrentDirectory = "C:\\Users\\thomas\\Desktop\\testx";

            //args = new[] { "orm", "sync", "--flags", "useNullables" };
            //args = new[] { "orm", "new", "-v", "sqlserver", "-c", "server=.;database=gerstl_120922;trusted_connection=true;encrypt=false", "-f", @"c:\users\thomas\desktop\Database.orm" };
            args = new[] { "orm", "sync", "-f", @"C:\Users\thomas\Desktop\Database.orm" };
            //args = new[] { "orm", "transform", "-f", @"c:\users\thomas\desktop\Database.orm" };
            //args = new[] { "orm", "run", "-f", @"c:\users\thomas\desktop\Database.orm", "--snippet", "test" };
            //args = new[] { "orm", "new", "-v", "sqlserver", "-c", "server=.;database=realescort_live;trusted_connection=true;encrypt=false" };

            RootCommand rootCommand = new RootCommand();

            rootCommand.AddGlobalOption(VerboseOption);

            new OrmCommandBuilder().Build(rootCommand);
            new RazorCommandBuilder().Build(rootCommand);

            return await rootCommand.InvokeAsync(args);
        }

        public static void WriteHeader()
        {
            NuGetVersion version = typeof(DotNetHost).Assembly.GetNuGetPackageVersion();

            if (version == null)
                WriteLine($"Jerrycurl CLI", ConsoleColor.Blue);
            else if (version.CommitHash != null)
                WriteLine($"Jerrycurl CLI v{version.PublicVersion} ({version.CommitHash})", ConsoleColor.Blue);
            else
                WriteLine($"Jerrycurl CLI v{version.PublicVersion}", ConsoleColor.Blue);

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