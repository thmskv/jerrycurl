using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jerrycurl.CodeAnalysis.Razor.Generation;
using Jerrycurl.CodeAnalysis.Razor.Parsing;
using Jerrycurl.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Jerrycurl.CodeAnalysis.Razor.Test;

internal static class GeneratorHelper
{
    public static string ProjectDirectory { get; } = Path.Combine(Path.GetTempPath(), "jerrycurl-razor-test");

    /// <summary>
    /// Parses and generates the given pages (project-relative path → content) and returns the generated C# per page path.
    /// </summary>
    public static IDictionary<string, string> Generate(params (string Path, string Content)[] files) => Generate("internal", files);

    public static IDictionary<string, string> Generate(string defaultVisibility, params (string Path, string Content)[] files)
    {
        RazorProject project = new RazorProject()
        {
            ProjectDirectory = ProjectDirectory,
            RootNamespace = "TestApp",
            IntermediateDirectory = null,
        };

        foreach (var (path, content) in files)
            project.AddItem(System.IO.Path.Combine(ProjectDirectory, path), content: content);

        RazorGenerator generator = new RazorGenerator(new RazorGeneratorOptions()
        {
            TemplateCode = RazorTemplates.Page,
            DefaultVisibility = defaultVisibility,
            Imports = [.. Jerrycurl.Facts.RazorFacts.DefaultNamespaces.Select(ns => new RazorFragment() { Text = ns })],
        });

        return new RazorParser().Parse(project).ToDictionary(p => p.ProjectPath.Replace('\\', '/'), p => generator.Generate(p.Data).Content);
    }

    public static string GenerateOne(string content, string path = "Queries/Page.cssql") => Generate((path, content)).Values.Single();

    public static IList<RazorDiagnostic> Diagnose(string content, string path = "Queries/Page.cssql")
    {
        RazorProject project = new RazorProject() { ProjectDirectory = ProjectDirectory, RootNamespace = "TestApp", IntermediateDirectory = null };

        project.AddItem(Path.Combine(ProjectDirectory, path), content: content);

        RazorPage page = new RazorParser().Parse(project).Single();

        new RazorGenerator(new RazorGeneratorOptions() { TemplateCode = RazorTemplates.Page }).Generate(page.Data);

        return page.Data.Diagnostics;
    }

    public static IList<Diagnostic> Compile(params string[] sources)
    {
        string tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        IEnumerable<MetadataReference> references = tpa.Split(Path.PathSeparator).Select(p => MetadataReference.CreateFromFile(p));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "Generated",
            sources.Select(s => CSharpSyntaxTree.ParseText(s)),
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Disable));

        return [.. compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error)];
    }
}
