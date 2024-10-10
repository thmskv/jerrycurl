using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Jerrycurl.Tools.Razor;

namespace Jerrycurl.Tools.DotNet.Cli.Commands;

internal class RazorCommandBuilder : ICommandBuilder
{
    public Option<string> ProjectOption { get; private set; }
    public Option<string> RootNamespaceOption { get; private set; }
    public Option<string> OutputOption { get; private set; }
    public Option<string[]> DirectoryOption { get; private set; }
    public Option<string[]> FileOption { get; private set; }
    public Option<string[]> ImportOption { get; private set; }
    public Option<bool> NoCleanOption { get; private set; }

    public RazorCommandBuilder()
    {
        this.CreateOptions();

    }
    private void CreateOptions()
    {
        this.ProjectOption = Option<string>(["--project", "-p"], ""); ;
        this.RootNamespaceOption = Option<string>(["--namespace", "-ns"], ""); ;
        this.OutputOption = Option<string>(["--output", "-o"], "");
        this.DirectoryOption = Option<string[]>(["--directory", "-d"], "", multiple: true);
        this.FileOption = Option<string[]>(["--file", "-f"], "", multiple: true);
        this.ImportOption = Option<string[]>(["--import", "-i"], "", multiple: true);
        this.NoCleanOption = Option<bool>(["--no-clean"], "");

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
        var command = new Command("cssql", "Transpiles collections of Razor SQL files into C# classes.")
        {
            { this.ProjectOption, this.RootNamespaceOption, this.OutputOption, this.DirectoryOption, this.FileOption, this.ImportOption, this.NoCleanOption }
        };

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
