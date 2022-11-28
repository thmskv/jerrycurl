using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.CommandLine.Invocation;
using Jerrycurl.CodeAnalysis.Projection;
using Jerrycurl.CodeAnalysis.Razor.Generation;
using Jerrycurl.CodeAnalysis.Razor.Parsing;
using Jerrycurl.CodeAnalysis.Razor.ProjectSystem;
using Jerrycurl.Facts;
using Jerrycurl.IO;
using System.Diagnostics;
using Jerrycurl.Tools.Resources;
using Jerrycurl.Tools.Orm;
using Jerrycurl.Tools.Vendors;
using Jerrycurl.Tools.Razor;

namespace Jerrycurl.Tools.DotNet.Cli.Commands
{
    internal class RazorCommandBuilder : ICommandBuilder
    {
        public Option<string> ProjectOption { get; private set; }
        public Option<string> RootNamespaceOption { get; private set; }
        public Option<string> OutputOption { get; private set; }
        public Option<IEnumerable<string>> DirectoryOption { get; private set; }
        public Option<IEnumerable<string>> FileOption { get; private set; }
        public Option<IEnumerable<string>> ImportOption { get; private set; }
        public Option<bool> NoCleanOption { get; private set; }

        public RazorCommandBuilder()
        {
            this.CreateOptions();

        }
        private void CreateOptions()
        {
            this.ProjectOption = Option<string>(new[] { "--project", "-p" }, ""); ;
            this.RootNamespaceOption = Option<string>(new[] { "--namespace", "-ns" }, ""); ;
            this.OutputOption = Option<string>(new[] { "--output", "-o" }, "");
            this.DirectoryOption = Option<IEnumerable<string>>(new[] { "--directory", "-d" }, "", multiple: true);
            this.FileOption = Option<IEnumerable<string>>(new[] { "--file", "-f" }, "", multiple: true);
            this.ImportOption = Option<IEnumerable<string>>(new[] { "--import", "-i" }, "", multiple: true);
            this.NoCleanOption = Option<bool>(new[] { "--no-clean" }, "");

            Option<T> Option<T>(string[] aliases, string description, bool multiple = false)
            {
                var option = new Option<T>(aliases[0], description)
                {
                    AllowMultipleArgumentsPerToken = multiple,
                };

                foreach (var alias in aliases.Skip(1))
                    option.AddAlias(alias);

                return option;
            }
        }

        public void Build(RootCommand rootCommand)
        {
            var command = new Command("cssql", "Transpiles collections of Razor SQL files into C# classes.");

            command.Add(this.ProjectOption, this.RootNamespaceOption, this.OutputOption, this.DirectoryOption, this.FileOption, this.ImportOption, this.NoCleanOption);

            this.SetHandler(command, async options =>
            {
                await RazorTool.GenerateAsync(options, new ToolConsole());
            });

            rootCommand.Add(command);
        }

        private void SetHandler(Command command, Func<RazorToolOptions, Task> handler)
        {
            command.SetHandler(async context =>
            {
                RazorToolOptions options = new RazorToolOptions()
                {
                    Directories = context.GetValue(this.DirectoryOption).ToList(),
                    Files = context.GetValue(this.FileOption).ToList(),
                    OutputDirectory = context.GetValue(this.OutputOption),
                    ProjectDirectory = context.GetValue(this.ProjectOption),
                    Imports = context.GetValue(this.ImportOption).ToList(),
                    NoClean = context.GetValue(this.NoCleanOption),
                    RootNamespace = context.GetValue(this.RootNamespaceOption),
                };

                await handler(options);
            });
        }
    }
}
