using System;
using Jerrycurl.CodeAnalysis.Razor.Generation;
using Jerrycurl.CodeAnalysis.Razor.Parsing;
using Jerrycurl.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis;
using System.IO;
using Jerrycurl.Facts;
using System.Linq;

namespace Jerrycurl.CodeAnalysis.Razor.Compiler;

[Generator]
public class RazorSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sourceItems = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".cssql", StringComparison.OrdinalIgnoreCase))
            .Select(static (file, cancellationToken) =>
            {
                var text = file.GetText(cancellationToken);

                return (
                    file.Path,
                    Content: text.ToString(),
                    Checksum: string.Join("", text.GetChecksum().Select(b => $"{b:x2}"))
                );
            })
            .Combine(context.AnalyzerConfigOptionsProvider);

        var importFiles = sourceItems.Where(static file =>
        {
            var path = file.Left.Path;

            if (string.Equals(Path.GetFileNameWithoutExtension(path), "_imports", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        });

        var finalItems = sourceItems.Combine(importFiles.Collect());


        context.RegisterSourceOutput(
            finalItems,
            (context, data) =>
            {
                var additionalText = data.Left.Left;
                var config = data.Left.Right;
                var imports = data.Right;

                try
                {
                    Go();
                }
                catch (Exception ex)
                {
                    var guid = $"{Guid.NewGuid()}";

                    context.AddSource($"jerry+{guid}.log", "/*" + ex.ToString() + "*/");
                    context.AddSource($"jerry+{guid}.alt.log", "/*" + string.Join("\n", additionalText.Path) + "*/");
                }


                //context.AddSource(file.FileName, file.Code);

                void Go()
                {
                    config.GlobalOptions.TryGetValue("build_property.ProjectDir", out var projectDirectory);
                    config.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);

                    var project = new RazorProject()
                    {
                        ProjectDirectory = projectDirectory,
                        RootNamespace = rootNamespace,
                    };

                    foreach (var (importText, _) in imports)
                        project.AddItem(importText.Path, content: importText.Content, checksum: importText.Checksum);

                    project.AddItem(additionalText.Path, content: additionalText.Content, checksum: additionalText.Checksum);

                    var parser = new RazorParser();
                    var generator = new RazorGenerator(new RazorGeneratorOptions()
                    {
                        TemplateCode = RazorTemplates.Page,
                        Imports = [.. RazorFacts.DefaultNamespaces.Select(ns => new RazorFragment() { Text = ns })],
                    });

                    var parsed = parser.Parse(project);

                    foreach (var razorPage in parsed)
                    {
                        var hintName = CSharp.Identifier(razorPage.ProjectPath) + ".g";
                        var projectionResult = generator.Generate(razorPage.Data);
                        var code = projectionResult.Content;

                        context.AddSource(hintName, code);
                    }
                }
            }
        );
    }
}
