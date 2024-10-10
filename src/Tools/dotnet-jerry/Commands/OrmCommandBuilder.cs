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
using Jerrycurl.Tools.Orm;
using System.Data.Common;
using Jerrycurl.Tools.Vendors;
using Jerrycurl.Tools.Resources;
using System.Diagnostics;
using Jerrycurl.IO;
using System.Xml;
using Jerrycurl.Tools.Orm.Model;

namespace Jerrycurl.Tools.DotNet.Cli.Commands;

internal class OrmCommandBuilder : ICommandBuilder
{
    public const string DefaultFileName = "Database.orm";

    public Argument<string> ConfigArgument { get; set; }
    //public Option<string> InputOption { get; private set; }
    public Option<string> VendorOption { get; private set; }
    public Option<string> ConnectionOption { get; private set; }
    public Option<string> NamespaceOption { get; private set; }
    public Option<string> TransformOption { get; private set; }
    public Option<string> OutputOption { get; private set; }
    public Option<string[]> FlagsOption { get; private set; }

    public OrmCommandBuilder()
    {
        this.CreateOptions();
    }

    private void CreateOptions()
    {
        this.ConfigArgument = new Argument<string>("config", description: "The .orm file to read configuration from.", getDefaultValue: () => DefaultFileName);
        this.VendorOption = this.Option<string>(["--vendor", "-v"], "The database vendor to target.");
        this.ConnectionOption = this.Option<string>(["--connection", "-c"], "The connection string to use.");
        this.NamespaceOption = this.Option<string>(["--namespace", "-ns"], "The namespace to place C# classes in.");
        this.OutputOption = this.Option<string>(["--output", "-o"], "The .cs file to place generated classes in.");
        this.TransformOption = this.Option<string>(["--transform", "-t"], "The .js file used for transforming output.");
        this.FlagsOption = this.Option<string[]>(["--flags"], "Flags to pass on to the configuration.");
    }

    public void Build(RootCommand rootCommand)
    {
        var command = new Command("orm", "Interacts with a database using an .orm configuration file.")
        {
            this.GetSyncCommand(),
            this.GetNewCommand(),
            this.GetDiffCommand(),
            this.GetRunCommand(),
            this.GetTransformCommand()
        };

        rootCommand.Add(command);
    }

    private Command GetTransformCommand()
    {
        Option<bool> openOption = this.Option<bool>(["--open"], "Open the .js file after creating using the default editor.");
        Option<bool> noTypesOption = this.Option<bool>(["--no-types"], "Do not create an associated d.ts file.");

        Command command = new Command("transform", "Create a .js transformation file for an .orm configuration.")
        {
            this.ConfigArgument, this.TransformOption, openOption, noTypesOption,
        };

        this.SetHandler(command, async (ctx, _, options) =>
        {
            bool open = ctx.GetValue(openOption);
            bool noTypes = ctx.GetValue(noTypesOption);

            string jsPath = options.Transform ?? $"{options.Input}.js";
            string tsPath = Path.Combine(Path.GetDirectoryName(jsPath), $"{Path.GetFileNameWithoutExtension(jsPath)}.d.ts");
            string tsName = noTypes ? null : Path.GetFileName(tsPath);

            if (!File.Exists(jsPath))
                await File.WriteAllTextAsync(jsPath, ResourceHelper.GetTransformJavaScriptTemplateString(typesFileName: tsName));

            if (!noTypes)
                await File.WriteAllTextAsync(tsPath, ResourceHelper.GetTransformTypeScriptTemplateString());

            if (open)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    UseShellExecute = true,
                    FileName = jsPath,
                };

                Process.Start(startInfo);
            }
        });

        return command;
    }

    private Command GetRunCommand()
    {
        Option<string> snippetOption = this.Option<string>(["--snippet"], "Name of a snippet to execute.");
        Option<string> sqlOption = this.Option<string>(["--sql"], "File to read and execute SQL from.");
        Option<string> textOption = this.Option<string>(["--text"], "SQL string to execute.");

        Command command = new Command("run", "Run SQL queries and commands against a database.")
        {
            this.ConfigArgument, this.ConnectionOption, this.VendorOption, this.TransformOption, snippetOption, sqlOption, textOption
        };

        this.SetHandler(command, async (ctx, tool, options) =>
        {
            string snippetValue = ctx.GetValue(snippetOption);
            string textValue = ctx.GetValue(textOption);
            string sqlValue = ctx.GetValue(sqlOption);

            string sql = options.Snippets?.GetValueOrDefault(snippetValue) ?? textValue ?? await File.ReadAllTextAsync(sqlValue);

            await using DbConnection connection = await tool.OpenConnectionAsync(options, new ToolConsole());

            using DbCommand command = connection.CreateCommand();

            command.CommandText = sql;

            DotNetHost.WriteLine($"Executing...", ConsoleColor.Yellow);
            DotNetHost.WriteLine(sql, ConsoleColor.DarkRed);

            await foreach (var tuple in tool.QueryAsync(command))
                Console.WriteLine(tuple.Serialize());
        });

        return command;
    }

    private Command GetDiffCommand()
    {
        Command command = new Command("diff", "Run a simple diff to check that your current C# classes matches the database schema.")
        {
            this.ConfigArgument, this.ConnectionOption, this.VendorOption, this.NamespaceOption, this.OutputOption, this.TransformOption, this.FlagsOption
        };

        this.SetHandler(command, async (_, tool, options) =>
        {
            string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "diff.right.cs");
            string leftPath = Path.Combine(tempPath, "diff.left.cs");
            string rightPath = Path.Combine(tempPath, "diff.right.cs");

            Directory.CreateDirectory(tempPath);

            if (File.Exists(options.Output))
                File.Copy(options.Output, leftPath);
            else
                await File.WriteAllTextAsync(leftPath, "");

            await this.BuildAndWriteAsync(tool, options, rightPath);

            byte[] leftBytes = await File.ReadAllBytesAsync(leftPath);
            byte[] rightBytes = await File.ReadAllBytesAsync(rightPath);

            if (leftBytes.SequenceEqual(rightBytes))
                DotNetHost.WriteLine("Files are equal", ConsoleColor.Green);
            else
                DotNetHost.WriteLine("Files are not equal", ConsoleColor.Red);
        });

        return command;
    }

    private Command GetNewCommand()
    {
        Option<bool> syncOption = this.Option<bool>(["--sync"], "Generate C# classes after creation.");

        Command command = new Command("new", "Create a new .orm configuration file.")
        {
            this.ConfigArgument, this.ConnectionOption, this.VendorOption, this.NamespaceOption, this.OutputOption, this.TransformOption, this.FlagsOption, syncOption,
        };

        this.SetHandler(command, async (ctx, tool, options) =>
        {
            bool sync = ctx.GetValue(syncOption);

            PathHelper.EnsureDirectory(options.Input);

            var data = new Dictionary<string, object>()
            {
                ["$schema"] = "https://raw.githubusercontent.com/thmskv/jerrycurl/main/eng/orm.schema.json",
                ["vendor"] = options.Vendor,
                ["connection"] = options.Connection,
                ["transform"] = options.Transform,
                ["output"] = options.Output,
                ["namespace"] = options.Namespace,
            };
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions()
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            });

            await File.WriteAllTextAsync(options.Input, json, Encoding.UTF8);

            if (sync)
            {
                options.Output ??= $"{options.Input}.cs";
                options.Transform ??= $"{options.Input}.js";

                await this.BuildAndWriteAsync(tool, options);
            }
        });

        return command;
    }

    private Command GetSyncCommand()
    {
        Command command = new Command("sync", "Generate C# classes from a database schema.")
        {
            this.ConfigArgument, this.ConnectionOption, this.VendorOption, this.NamespaceOption, this.OutputOption, this.TransformOption, this.FlagsOption
        };

        this.SetHandler(command, async (_, tool, options) =>
        {
            options.Output ??= $"{options.Input}.cs";
            options.Transform ??= $"{options.Input}.js";

            await this.BuildAndWriteAsync(tool, options);
        });

        return command;
    }

    private Option<T> Option<T>(string[] aliases, string description, string defaultValue = null, bool multiple = false)
    {
        var option = new Option<T>(aliases[0], description)
        {
            AllowMultipleArgumentsPerToken = multiple,
        };

        if (defaultValue != null)
            option.SetDefaultValue(defaultValue);

        foreach (var alias in aliases.Skip(1))
            option.AddAlias(alias);

        return option;
    }

    private async Task BuildAndWriteAsync(OrmTool tool, OrmToolOptions options, string outputPath = null)
    {
        SchemaModel schema = await tool.BuildAsync(options, new ToolConsole());

        await tool.WriteAsync(schema, options, new ToolConsole());
    }
    private void SetHandler(Command command, Func<InvocationContext, OrmTool, OrmToolOptions, Task> handler)
    {
        command.SetHandler(async context =>
        {
            string configValue = context.GetValue(this.ConfigArgument);
            string vendorValue = context.GetValue(this.VendorOption);
            string connectionValue = context.GetValue(this.ConnectionOption);
            string outputValue = context.GetValue(this.OutputOption);
            string namespaceValue = context.GetValue(this.NamespaceOption);
            string transformValue = context.GetValue(this.TransformOption);
            bool verboseValue = context.GetValue(DotNetHost.VerboseOption);
            string[] flags = context.GetValue(this.FlagsOption);

            OrmToolOptions options = new OrmToolOptions()
            {
                Input = configValue ?? DefaultFileName,
            };

            if (File.Exists(configValue))
            {
                try
                {
                    options = await OrmToolOptions.FromFileAsync(configValue);
                }
                catch (Exception ex)
                {
                    throw new OrmToolException($"{Path.GetFileName(configValue)} is not a valid configuration file.", innerException: ex);
                }
            }
            else if (command.Name != "new" && (options.Input != DefaultFileName || (connectionValue == null || vendorValue == null)))
                throw new OrmToolException($"{configValue} not found.");

            options.Vendor = vendorValue ?? options.Vendor;
            options.Connection = connectionValue ?? options.Connection;
            options.Output = outputValue ?? options.Output;
            options.Namespace = namespaceValue ?? options.Namespace;
            options.Transform = transformValue ?? options.Transform;
            options.Flags ??= [];
            options.Verbose = verboseValue;

            foreach (var flag in flags)
            {
                string[] pair = flag.Split("=");

                if (pair.Length == 1)
                    options.Flags[pair[0]] = "true";
                else
                    options.Flags[pair[0]] = pair[1];
            }

            OrmTool tool = ToolResolver.GetOrmTool(options.Vendor);

            await handler(context, tool, options);
        });
    }
}
