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
using Jerrycurl.Tools.Loader;
using Jerrycurl.Tools.Orm.Model;
using Jerrycurl.Tools.Orm;
using Jerrycurl.Reflection;
using Jerrycurl.Tools.DotNet.Cli.Commands;
using Jerrycurl.Tools.DotNet.Cli.Runners;
using System.Data.Common;
using Jerrycurl.Tools.Vendors.SqlServer;
using Jerrycurl.Tools.Vendors;
using Jerrycurl.Tools.Resources;
using System.Diagnostics;

namespace Jerrycurl.Tools.DotNet.Cli.V2
{
    internal class OrmCommandBuilder : ICommandBuilder
    {
        public const string DefaultFileName = "Database.orm";

        public Option<string> FileOption { get; private set; }
        public Option<string> VendorOption { get; private set; }
        public Option<string> ConnectionOption { get; private set; }
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
            this.NoFileOption = this.Option<bool>(new[] { "--no-file" }, "Do not load configuration from the default Database.orm file.");
        }

        private Option<T> Option<T>(string[] aliases, string description, string defaultValue = null)
        {
            var option = new Option<T>(aliases[0], description);

            option.SetDefaultValue(defaultValue);

            foreach (var alias in aliases.Skip(1))
                option.AddAlias(alias);

            return option;
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
            Command command = new Command("run", "Run a simple diff between your C# classes and database.");

            command.Add(this.FileOption);
            command.Add(this.ConnectionOption);
            command.Add(this.VendorOption);
            command.Add(this.NoFileOption);

            command.SetHandler(Handler, this.FileOption, this.ConnectionOption, this.VendorOption, this.NoFileOption);

            return command;

            async Task Handler(string file, string connection, string vendor, bool noFile)
            {
                OrmModel ormFile = await this.ReadFileAsync(file, noFile);

                ormFile.Connection = connection ?? ormFile.Connection;
                //ormFile.Vendor = vendor ?? ormFile.Vendor;
            };
        }

        private Command GetDiffCommand()
        {
            Command command = new Command("diff", "Run a simple diff to check that your current C# classes matches the database schema.");

            command.Add(this.FileOption);
            command.Add(this.ConnectionOption);
            command.Add(this.VendorOption);
            command.Add(this.NoFileOption);

            command.SetHandler(Handler, this.FileOption, this.ConnectionOption, this.VendorOption, this.NoFileOption);

            return command;

            async Task<int> Handler(string file, string connection, string vendor, bool noFile)
            {
                OrmModel ormFile = await this.ReadFileAsync(file, noFile);

                ormFile.Connection = connection ?? ormFile.Connection;
                ormFile.Output ??= $"{file}.cs";
                ormFile.Vendor = vendor ?? ormFile.Vendor;

                string tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                string leftPath = Path.Combine(tempPath, "diff.left.cs");
                string rightPath = Path.Combine(tempPath, "diff.right.cs");

                Directory.CreateDirectory(tempPath);

                if (File.Exists(ormFile.Output))
                    File.Copy(ormFile.Output, leftPath);
                else
                    await File.WriteAllTextAsync(leftPath, "");

                await this.GenerateCodeAsync(ormFile, rightPath);

                byte[] leftBytes = await File.ReadAllBytesAsync(leftPath);
                byte[] rightBytes = await File.ReadAllBytesAsync(rightPath);

                if (leftBytes.SequenceEqual(rightBytes))
                {
                    DotNetJerryHostV2.WriteLine("Files are equal", ConsoleColor.Green);

                    return 0;
                }
                else
                {
                    DotNetJerryHostV2.WriteLine("Files are not equal", ConsoleColor.Red);

                    return -484;
                }
            };
        }

        private async Task GenerateCodeAsync(OrmModel ormFile, string outputPath = null)
        {
            var ormCommand = VendorResolver.GetOrmCommand(ormFile.Vendor);

            using var dbConnection = await this.GetOpenConnectionAsync(ormFile, ormCommand);

            var types = ormCommand.GetTypeMappings();

            var model = await ormCommand.GetDatabaseModelAsync(dbConnection);

            ormCommand.ApplyDefaults(ormFile, model, types.ToList());

            var transformer = new OrmTransformer();
            var writer = new OrmCodeWriter();

            model = await transformer.TransformAsync(ormFile, model);

            await writer.WriteDefaultAsync(ormFile, model, outputPath);
        }

        private Command GetSyncCommand()
        {
            Command command = new Command("sync", "Generate C# classes from a database schema.");

            command.Add(this.FileOption);
            command.Add(this.ConnectionOption);
            command.Add(this.VendorOption);
            command.Add(this.NoFileOption);

            command.SetHandler(Handler, this.FileOption, this.ConnectionOption, this.VendorOption, this.NoFileOption);

            return command;

            async Task Handler(string file, string connection, string vendor, bool noFile)
            {
                OrmModel ormFile = await this.ReadFileAsync(file, noFile);

                ormFile.Connection = connection ?? ormFile.Connection;
                ormFile.Output ??= $"{file}.cs";
                ormFile.Vendor = vendor ?? ormFile.Vendor;

                var ormCommand = VendorResolver.GetOrmCommand(ormFile.Vendor);

                using var dbConnection = await this.GetOpenConnectionAsync(ormFile, ormCommand);

                var types = ormCommand.GetTypeMappings();

                var model = await ormCommand.GetDatabaseModelAsync(dbConnection);

                ormCommand.ApplyDefaults(ormFile, model, types.ToList());

                var transformer = new OrmTransformer();
                var writer = new OrmCodeWriter();

                model = await transformer.TransformAsync(ormFile, model);

                await writer.WriteDefaultAsync(ormFile, model);
            };
        }

        private async Task<DbConnection> GetOpenConnectionAsync(OrmModel ormFile, OrmCommand factory)
        {
            DbConnection connection = factory.GetDbConnection();

            if (connection == null)
                throw new RunnerException("Connection returned null.");

            try
            {
                connection.ConnectionString = ormFile.Connection;
            }
            catch (Exception ex)
            {
                connection.Dispose();

                throw new RunnerException("Invalid connection string: " + ex.Message, ex);
            }

            if (!string.IsNullOrEmpty(connection.Database))
                DotNetJerryHost.WriteLine($"Connecting to '{connection.Database}'...", ConsoleColor.Yellow);
            else
                DotNetJerryHost.WriteLine("Connecting to database...", ConsoleColor.Yellow);

            try
            {
                await connection.OpenAsync().ConfigureAwait(false);

                return connection;
            }
            catch (Exception ex)
            {
                connection.Dispose();

                throw new RunnerException("Unable to open connection: " + ex.Message, ex);
            }
        }

        private Command GetNewCommand()
        {
            Command command = new Command("new", "Create a new .orm configuration file.");

            command.Add(this.FileOption);
            command.Add(this.ConnectionOption);
            command.Add(this.VendorOption);

            command.SetHandler(Handler, this.FileOption, this.ConnectionOption, this.VendorOption);

            return command;

            async Task Handler(string file, string connection, string vendor)
            {
                string fullPath = Path.GetFullPath(file ?? DefaultFileName);

                if (!Directory.Exists(Path.GetDirectoryName(fullPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                var config = new
                {
                    vendor = vendor ?? "",
                    connection = connection ?? "",
                };
                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                });

                await File.WriteAllTextAsync(fullPath, json, Encoding.UTF8);
            };
        }

        private async Task<OrmModel> ReadFileAsync(string file, bool noFile)
        {
            if (noFile)
                return new OrmModel();

            string fullPath = Path.GetFullPath(file ?? DefaultFileName);

            return await OrmModel.FromFileAsync(fullPath);
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
    }
}
