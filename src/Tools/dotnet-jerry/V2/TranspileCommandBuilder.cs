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
using Jerrycurl.CodeAnalysis.Projection;
using Jerrycurl.CodeAnalysis.Razor.Generation;
using Jerrycurl.CodeAnalysis.Razor.Parsing;
using Jerrycurl.CodeAnalysis.Razor.ProjectSystem;
using Jerrycurl.Facts;
using Jerrycurl.IO;
using Jerrycurl.Tools.DotNet.Cli.Commands;
using Jerrycurl.Tools.DotNet.Cli.Runners;
using System.Diagnostics;
using Jerrycurl.Tools.Resources;

namespace Jerrycurl.Tools.DotNet.Cli.V2
{
    internal class TranspileCommandBuilder : ICommandBuilder
    {
        public void Build(RootCommand rootCommand)
        {
            var command = new Command("cssql", "Transpiles collections of Razor SQL files into C# classes.");

            Option<string> projectOption = Option<string>(new[] { "--project", "-p" }, "");
            Option<string> namespaceOption = Option<string>(new[] { "--namespace", "-ns" }, "");
            Option<string> outputOption = Option<string>(new[] { "--output", "-o" }, "");
            Option<IEnumerable<string>> directoryOption = Option<IEnumerable<string>>(new[] { "--directory", "-d" }, "", multiple: true);
            Option<IEnumerable<string>> fileOption = Option<IEnumerable<string>>(new[] { "--file", "-f" }, "", multiple: true);
            Option<IEnumerable<string>> importOption = Option<IEnumerable<string>>(new[] { "--import", "-i" }, "", multiple: true);
            Option<bool> noCleanOption = Option<bool>(new[] { "--no-clean" }, "");

            command.Add(projectOption);
            command.Add(namespaceOption);
            command.Add(outputOption);
            command.Add(directoryOption);
            command.Add(fileOption);
            command.Add(importOption);
            command.Add(noCleanOption);
            command.SetHandler(this.RunAsync, projectOption, namespaceOption, outputOption, directoryOption, fileOption, importOption, noCleanOption);

            rootCommand.Add(command);

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

        private async Task RunAsync(string projectOption, string @namespace, string output, IEnumerable<string> directories, IEnumerable<string> files, IEnumerable<string> imports, bool noClean)
        {
            string projectDirectory = projectOption ?? Environment.CurrentDirectory;
            string rootNamespace = @namespace;
            string sourcePath = Path.GetDirectoryName(typeof(DotNetJerryHostV2).Assembly.Location);
            string outputDirectory = output;

            if (!Directory.Exists(projectDirectory))
                throw new RunnerException($"Project directory '{projectDirectory}' does not exist.");

            projectDirectory = PathHelper.MakeAbsolutePath(Environment.CurrentDirectory, projectDirectory);
            outputDirectory = PathHelper.MakeAbsolutePath(projectDirectory, outputDirectory ?? RazorProjectConventions.DefaultIntermediateDirectory);

            RazorProject project = new RazorProject()
            {
                ProjectDirectory = projectDirectory,
                RootNamespace = rootNamespace,
                Items = new List<RazorProjectItem>(),
                IntermediateDirectory = outputDirectory,
            };

            foreach (string file in files)
            {
                if (!HasPipeFormat(file, out var fullPath, out var projectPath))
                    project.AddItem(file);
                else if (!string.IsNullOrEmpty(fullPath))
                    project.Items.Add(new RazorProjectItem() { FullPath = MakeAbsolutePath(fullPath), ProjectPath = projectPath });
            }

            foreach (string dir in directories)
            {
                RazorProject fromDir = RazorProject.FromDirectory(MakeAbsolutePath(dir));

                foreach (RazorProjectItem item in fromDir.Items)
                    project.Items.Add(item);
            }

            RazorGeneratorOptions options = new RazorGeneratorOptions()
            {
                TemplateCode = ResourceHelper.GetRazorSkeletonString(),
                Imports = RazorFacts.DefaultNamespaces.Select(ns => new RazorFragment() { Text = ns }).ToList(),
            };

            foreach (string import in imports)
                options.Imports.Add(new RazorFragment() { Text = import });


            if (!noClean && Directory.Exists(outputDirectory))
            {
                DotNetJerryHostV2.WriteLine("Cleaning...", ConsoleColor.Yellow);

                foreach (string oldFile in Directory.GetFiles(outputDirectory, "*.cssql.cs"))
                    File.Delete(oldFile);
            }

            if (project.Items.Any())
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                Directory.CreateDirectory(outputDirectory);

                RazorParser parser = new RazorParser();
                RazorGenerator generator = new RazorGenerator(options);

                DotNetJerryHostV2.WriteLine("Parsing...", ConsoleColor.Yellow);
                IList<RazorPage> parserResult = parser.Parse(project).ToList();

                DotNetJerryHostV2.WriteLine("Transpiling...", ConsoleColor.Yellow);
                foreach (RazorPage razorPage in parserResult)
                {
                    ProjectionResult result = generator.Generate(razorPage.Data);

                    await File.WriteAllTextAsync(razorPage.IntermediatePath, result.Content);
                }

                string filesString = parserResult.Count + " " + (parserResult.Count == 1 ? "file" : "files");
                string outputString = PathHelper.MakeRelativeOrAbsolutePath(project.ProjectDirectory, outputDirectory);

                DotNetJerryHostV2.WriteLine($"Transpiled {filesString} in {stopwatch.ElapsedMilliseconds:0} ms into '{outputString}'", ConsoleColor.Green);
            }
            else
                DotNetJerryHostV2.WriteLine($"No files found.", ConsoleColor.Yellow);

            string MakeAbsolutePath(string path) => PathHelper.MakeAbsolutePath(project.ProjectDirectory, path);

            bool HasPipeFormat(string input, out string fullPath, out string projectPath)
            {
                string[] parts = input.Split(new[] { '|' }, 2);

                if (parts.Length == 2)
                {
                    fullPath = parts[0];
                    projectPath = string.IsNullOrWhiteSpace(parts[1]) ? null : parts[1];

                    return true;
                }

                fullPath = projectPath = null;

                return false;
            }
        }
    }
}
