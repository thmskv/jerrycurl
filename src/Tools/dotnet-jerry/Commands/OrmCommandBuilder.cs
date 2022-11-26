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
using Jerrycurl.Tools.Orm.Model;
using Jerrycurl.Tools.Orm;
using Jerrycurl.Reflection;
using System.Data.Common;
using Jerrycurl.Tools.Vendors.SqlServer;
using Jerrycurl.Tools.Vendors;
using Jerrycurl.Tools.Resources;
using System.Diagnostics;
using System.Numerics;

namespace Jerrycurl.Tools.DotNet.Cli.Commands
{
    internal class OrmCommandBuilder : ICommandBuilder
    {
        public const string DefaultFileName = "Database.orm";

        public Option<string> FileOption { get; private set; }
        public Option<string> VendorOption { get; private set; }
        public Option<string> ConnectionOption { get; private set; }
        public Option<string> NamespaceOption { get; private set; }
        public Option<string> TransformOption { get; private set; }
        public Option<string> OutputOption { get; private set; }
        public Option<bool> NoFileOption { get; private set; }

        public OrmCommandBuilder()
        {
            this.CreateOptions();
        }

        private void CreateOptions()
        {
            this.FileOption = this.Option<string>(new[] { "--file", "-f" }, "The .orm file to read configuration from.", defaultValue: DefaultFileName);
            this.VendorOption = this.Option<string>(new[] { "--vendor", "-v" }, "The database vendor to target.");
            this.ConnectionOption = this.Option<string>(new[] { "--connection", "-c" }, "The connection string to use.");
            this.NamespaceOption = this.Option<string>(new[] { "--namespace", "-ns" }, "The namespace to place C# classes in.");
            this.OutputOption = this.Option<string>(new[] { "--output", "-o" }, "The .cs file to place generated classes in.");
            this.TransformOption = this.Option<string>(new[] { "--transform", "-t" }, "The .js file used for transforming output.");
            this.NoFileOption = this.Option<bool>(new[] { "--no-file" }, "Do not load configuration from the default Database.orm file.");
        }

        public void Build(RootCommand rootCommand)
        {
            var command = new Command("orm", "Interacts with a database using an .orm configuration file.");

            command.Add(this.GetSyncCommand());
            command.Add(this.GetNewCommand());
            command.Add(this.GetDiffCommand());
            command.Add(this.GetRunCommand());
            command.Add(this.GetTransformCommand());

            rootCommand.Add(command);
        }

        private Command GetTransformCommand()
        {
            Command command = new Command("transform", "Create a .js transformation file for an .orm configuration.");

            Option<bool> openOption = this.Option<bool>(new[] { "--open" }, "Open the .js file after creating using the default editor.");
            Option<bool> noTypesOption = this.Option<bool>(new[] { "--no-types" }, "Do not create an associated d.ts file.");

            command.Add(this.FileOption);
            command.Add(openOption);
            command.Add(noTypesOption);
            command.SetHandler(Handler, this.FileOption, openOption, noTypesOption);

            return command;

            async Task Handler(string file, bool open, bool noTypes)
            {
                string jsPath = file + ".js";
                string tsPath = file + ".d.ts";
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
            };
        }

        private Command GetRunCommand()
        {
            Command command = new Command("run", "Run SQL queries and commands against a database.");

            Option<string> snippetOption = this.Option<string>(new[] { "--snippet" }, "Name of a snippet to run.");

            command.Add(this.FileOption, this.ConnectionOption, this.VendorOption, this.NoFileOption, this.NamespaceOption, this.OutputOption, snippetOption);

            this.SetHandler(command, async (tool, options) =>
            {
                
            });

            return command;
        }

        private Command GetDiffCommand()
        {
            Command command = new Command("diff", "Run a simple diff to check that your current C# classes matches the database schema.");

            command.Add(this.FileOption, this.ConnectionOption, this.VendorOption, this.NoFileOption, this.NamespaceOption, this.OutputOption);

            this.SetHandler(command, async (tool, options) =>
            {
                string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                string leftPath = Path.Combine(tempPath, "diff.left.cs");
                string rightPath = Path.Combine(tempPath, "diff.right.cs");

                Directory.CreateDirectory(tempPath);

                if (File.Exists(options.Output))
                    File.Copy(options.Output, leftPath);
                else
                    await File.WriteAllTextAsync(leftPath, "");

                await tool.BuildAndOutputAsync(options, rightPath);

                byte[] leftBytes = await File.ReadAllBytesAsync(leftPath);
                byte[] rightBytes = await File.ReadAllBytesAsync(rightPath);

                if (leftBytes.SequenceEqual(rightBytes))
                    DotNetJerryHostV2.WriteLine("Files are equal", ConsoleColor.Green);
                else
                    DotNetJerryHostV2.WriteLine("Files are not equal", ConsoleColor.Red);
            });

            return command;
        }

        private Command GetNewCommand()
        {
            Command command = new Command("new", "Create a new .orm configuration file.");

            command.Add(this.FileOption, this.ConnectionOption, this.VendorOption, this.NoFileOption, this.NamespaceOption, this.OutputOption);

            this.SetHandler(command, async (_, options) =>
            {
                Directory.CreateDirectory(Path.GetDirectoryName(options.Output));

                string json = JsonSerializer.Serialize(options, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                });

                await File.WriteAllTextAsync(options.Input, json, Encoding.UTF8);
            });

            return command;
        }



        private Option<T> Option<T>(string[] aliases, string description, string defaultValue = null)
        {
            var option = new Option<T>(aliases[0], description);

            option.SetDefaultValue(defaultValue);

            foreach (var alias in aliases.Skip(1))
                option.AddAlias(alias);

            return option;
        }


        private void SetHandler(Command command, Func<OrmTool, OrmToolOptions, Task> handler)
        {
            command.SetHandler(async context =>
            {
                string fileValue = context.GetValue(this.FileOption);
                string vendorValue = context.GetValue(this.VendorOption);
                string connectionValue = context.GetValue(this.ConnectionOption);
                string outputValue = context.GetValue(this.OutputOption);
                string namespaceValue = context.GetValue(this.NamespaceOption);
                bool noFileValue = context.GetValue(this.NoFileOption);

                OrmToolOptions options = new OrmToolOptions()
                {
                    Input = fileValue,
                };

                if (!noFileValue && File.Exists(fileValue))
                    options = await OrmToolOptions.FromFileAsync(fileValue);

                options.Vendor = vendorValue ?? options.Vendor;
                options.Connection = connectionValue ?? options.Connection;
                options.Output = outputValue ?? options.Output ?? $"{options.Input}.cs";
                options.Transform ??= $"{options.Input}.js";
                options.Namespace = outputValue ?? options.Namespace;

                OrmTool tool = ToolResolver.GetOrmTool(options.Vendor);

                await handler(tool, options);
            });
        }

        private Command GetSyncCommand()
        {
            Command command = new Command("sync", "Generate C# classes from a database schema.");

            command.Add(this.FileOption, this.ConnectionOption, this.VendorOption, this.NoFileOption, this.NamespaceOption, this.OutputOption);

            this.SetHandler(command, async (tool, options) =>
            {
                await tool.BuildAndOutputAsync(options);
            });

            return command;
        }
    }
}
