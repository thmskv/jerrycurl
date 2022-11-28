using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jerrycurl.CodeAnalysis.Projection;
using Jerrycurl.CodeAnalysis.Razor.Generation;
using Jerrycurl.CodeAnalysis.Razor.Parsing;
using Jerrycurl.CodeAnalysis.Razor.ProjectSystem;
using Jerrycurl.Facts;
using Jerrycurl.IO;
using Jerrycurl.Tools.Resources;

namespace Jerrycurl.Tools.Razor
{
    public static class RazorTool
    {
        public static async Task GenerateAsync(RazorToolOptions options, ToolConsole console)
        {
            string projectDirectory = options.ProjectDirectory ?? Environment.CurrentDirectory;
            string rootNamespace = options.RootNamespace;
            string outputDirectory = options.OutputDirectory;

            if (!Directory.Exists(projectDirectory))
                throw new Exception($"Project directory '{projectDirectory}' does not exist.");

            projectDirectory = PathHelper.MakeAbsolutePath(Environment.CurrentDirectory, projectDirectory);
            outputDirectory = PathHelper.MakeAbsolutePath(projectDirectory, outputDirectory ?? RazorProjectConventions.DefaultIntermediateDirectory);

            RazorProject project = new RazorProject()
            {
                ProjectDirectory = projectDirectory,
                RootNamespace = rootNamespace,
                Items = new List<RazorProjectItem>(),
                IntermediateDirectory = outputDirectory,
            };

            console.Run("Collecting files", () =>
            {
                foreach (string file in options.Files)
                {
                    if (!HasVirtualFormat(file, out var fullPath, out var projectPath))
                        project.AddItem(file);
                    else if (!string.IsNullOrEmpty(fullPath))
                        project.Items.Add(new RazorProjectItem() { FullPath = MakeAbsolutePath(fullPath), ProjectPath = projectPath });
                }

                foreach (string dir in options.Directories)
                {
                    RazorProject fromDir = RazorProject.FromDirectory(MakeAbsolutePath(dir));

                    foreach (RazorProjectItem item in fromDir.Items)
                        project.Items.Add(item);
                }
            });

            if (!options.NoClean && Directory.Exists(outputDirectory))
            {
                console.Run("Cleaning output", () =>
                {
                    foreach (string oldFile in Directory.GetFiles(outputDirectory, "*.cssql.cs"))
                        File.Delete(oldFile);
                });
            }

            if (project.Items.Any())
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                Directory.CreateDirectory(outputDirectory);

                RazorParser parser = new RazorParser();
                IList<RazorPage> parserResult = console.Run("Parsing files", () => parser.Parse(project).ToList());

                RazorGeneratorOptions generatorOptions = new RazorGeneratorOptions()
                {
                    TemplateCode = ResourceHelper.GetRazorSkeletonString(),
                    Imports = RazorFacts.DefaultNamespaces.Select(ns => new RazorFragment() { Text = ns }).ToList(),
                };
                RazorGenerator generator = new RazorGenerator(generatorOptions);

                foreach (string import in options.Imports)
                    generatorOptions.Imports.Add(new RazorFragment() { Text = import });

                await console.RunAsync("Transpiling into C#", async () =>
                {
                    foreach (RazorPage razorPage in parserResult)
                    {
                        ProjectionResult result = generator.Generate(razorPage.Data);

                        using (StreamWriter writer = new StreamWriter(razorPage.IntermediatePath, append: false, Encoding.UTF8))
                            await writer.WriteAsync(result.Content);
                    }
                });

                string filesString = parserResult.Count + " " + (parserResult.Count == 1 ? "file" : "files");
                string outputString = PathHelper.MakeRelativeOrAbsolutePath(project.ProjectDirectory, outputDirectory);

                console.WriteLine($"Transpiled {filesString} in {stopwatch.ElapsedMilliseconds:0} ms into '{outputString}'", ConsoleColor.Green);
            }
            else
                console.WriteLine($"No files found.", ConsoleColor.Yellow);

            string MakeAbsolutePath(string path) => PathHelper.MakeAbsolutePath(project.ProjectDirectory, path);

            bool HasVirtualFormat(string input, out string fullPath, out string projectPath)
            {
                string[] parts = input.Split(new[] { ':' }, 2);

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
