using System;
using Jerrycurl.CodeAnalysis.Razor.Generation;
using Jerrycurl.CodeAnalysis.Razor.Parsing;
using Jerrycurl.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis;
using System.IO;
using Jerrycurl.Facts;
using System.Linq;
using Jerrycurl.CodeAnalysis.Lexing;
using Jerrycurl.CodeAnalysis.Razor.ProjectSystem.Conventions;
using Microsoft.CodeAnalysis.Text;

namespace Jerrycurl.CodeAnalysis.Razor.Compiler;

[Generator]
public class RazorSourceGenerator : IIncrementalGenerator
{
    private static Diagnostic ToDiagnostic(RazorDiagnostic diagnostic, string content)
    {
        DiagnosticSeverity severity = diagnostic.Severity switch
        {
            RazorDiagnosticSeverity.Error => DiagnosticSeverity.Error,
            RazorDiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
            _ => DiagnosticSeverity.Info,
        };

        DiagnosticDescriptor descriptor = new DiagnosticDescriptor(diagnostic.Id, diagnostic.Title ?? diagnostic.Id, "{0}", "Jerrycurl", severity, isEnabledByDefault: true);

        return Diagnostic.Create(descriptor, GetLocation(diagnostic, content), diagnostic.Message);
    }

    private static Location GetLocation(RazorDiagnostic diagnostic, string content)
    {
        if (diagnostic.SourceName == null)
            return Location.None;

        SourceSpan span = diagnostic.Span ?? new SourceSpan(0, 0, 1, 1);
        int line = Math.Max(span.Line, 1) - 1;
        int column = Math.Max(span.Column, 1) - 1;
        int length = content == null ? 0 : Math.Max(0, Math.Min(span.Length, content.Length - span.Start));

        TextSpan textSpan = new TextSpan(span.Start, length);
        LinePositionSpan lineSpan = new LinePositionSpan(new LinePosition(line, column), new LinePosition(line, column + length));

        return Location.Create(diagnostic.SourceName, textSpan, lineSpan);
    }

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
                    config.GlobalOptions.TryGetValue("build_property.JerrycurlPageVisibility", out var defaultVisibility);

                    if (string.IsNullOrWhiteSpace(defaultVisibility))
                        defaultVisibility = "internal";
                    else if (!RazorParser.IsValidVisibility(defaultVisibility))
                    {
                        RazorDiagnostic invalid = RazorParser.InvalidVisibility(defaultVisibility);

                        invalid.Message += " (from the JerrycurlPageVisibility MSBuild property)";

                        context.ReportDiagnostic(ToDiagnostic(invalid, null));

                        defaultVisibility = "internal";
                    }

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
                        DefaultVisibility = defaultVisibility,
                        Imports = [.. RazorFacts.DefaultNamespaces.Select(ns => new RazorFragment() { Text = ns })],
                    });

                    if (RazorImportConvention.IsImportPath(additionalText.Path))
                    {
                        var importData = parser.Parse(new StringSource(additionalText.Content), additionalText.Path, additionalText.Checksum);

                        foreach (var diagnostic in RazorImportConvention.Validate(importData))
                            context.ReportDiagnostic(ToDiagnostic(diagnostic, additionalText.Content));

                        return;
                    }

                    var parsed = parser.Parse(project);

                    foreach (var razorPage in parsed)
                    {
                        var hintName = CSharp.Identifier(razorPage.ProjectPath) + ".g";
                        var projectionResult = generator.Generate(razorPage.Data);
                        var code = projectionResult.Content;

                        context.AddSource(hintName, code);

                        foreach (var diagnostic in razorPage.Data.Diagnostics)
                        {
                            // Diagnostics inherited from an _imports file are reported when that file is processed.
                            if (diagnostic.SourceName == null || string.Equals(diagnostic.SourceName, additionalText.Path, StringComparison.OrdinalIgnoreCase))
                                context.ReportDiagnostic(ToDiagnostic(diagnostic, additionalText.Content));
                        }
                    }
                }
            }
        );
    }
}
