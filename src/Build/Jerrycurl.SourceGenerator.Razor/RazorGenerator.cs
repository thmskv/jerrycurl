using System;
using Jerrycurl.CodeAnalysis.Projection;
using Jerrycurl.CodeAnalysis.Razor.Generation;
using Jerrycurl.CodeAnalysis.Razor.Lexing.CSharp;
using Jerrycurl.CodeAnalysis.Razor.Parsing;
using Jerrycurl.CodeAnalysis.Razor.ProjectSystem;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Jerrycurl.IO;
using System.IO;
using Jerrycurl.CodeAnalysis;
using Jerrycurl.Facts;
using System.Linq;

namespace Jerrycurl.SourceGenerator.Razor;

[Generator]
public class RazorGeneratorX : IIncrementalGenerator
{
    private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.ParseOptionsProvider.Select(x => x.)
        var values = context.AdditionalTextsProvider.Combine(context.AnalyzerConfigOptionsProvider)
            .Where(x => x.Right.GetOptions(x.Left).TryGetValue(SourceItemGroupMetadata, out var itemGroup) && itemGroup == "JerryFile");

        context.RegisterSourceOutput(
            values,
            (context, data) =>
            {

                if (string.IsNullOrEmpty(data.Namespace))
                {
                    context.ReportDiagnostic(Diagnostic.Create(CommonDiagnostics.NoNamespace, Location.None, _manifestFileName));
                    return;
                }

                Manifest manifest;
                try
                {
                    manifest = ManifestParser.Parse(data.ManifestContents);
                }
                catch (InvalidManifestException ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(_invalidManifestFile, Location.None, ex.Message));
                    return;
                }

                GeneratedFile file = ManifestCodeWriter.Write(manifest, data.Namespace, data.LangVersion);
                context.AddSource(file.FileName, file.Code);
            }
        );
    }

    public bool Execute()
    {
        RazorProject project = this.CreateRazorProject();
        RazorParser parser = new RazorParser();

        List<string> filesToCompile = new List<string>();

        Stopwatch watch = Stopwatch.StartNew();

        foreach (RazorPage razorPage in parser.Parse(project))
        {
            RazorGenerator generator = new RazorGenerator(this.CreateGeneratorOptions());
            ProjectionResult result = generator.Generate(razorPage.Data);

            PathHelper.EnsureDirectory(razorPage.IntermediatePath);
            File.WriteAllText(razorPage.IntermediatePath, result.Content, Encoding.UTF8);

            filesToCompile.Add(razorPage.IntermediatePath);
        }
        this.Compile = filesToCompile.ToArray();

        return true;
    }

    private string GetFullyQualifiedPageName(RazorPage razorPage)
    {
        string escapedNs = Namespace.Escape(razorPage.Data.Namespace?.Text);
        string escapedClass = string.IsNullOrWhiteSpace(razorPage.Data.Class?.Text) ? "" : CSharp.Identifier(razorPage.Data.Class.Text.Trim());

        if (escapedClass.Length == 0 && escapedNs.Length == 0)
            return "<invalid>";
        else if (escapedNs.Length == 0)
            return $"global::{escapedClass}";
        else if (escapedClass.Length == 0)
            return $"{escapedNs}.<invalid>";
        else
            return $"{escapedNs}.{escapedClass}";
    }

    private RazorGeneratorOptions CreateGeneratorOptions()
    {
        string templateCode = null;

        if (File.Exists(this.SkeletonPath))
            templateCode = File.ReadAllText(this.SkeletonPath);

        return new RazorGeneratorOptions()
        {
            TemplateCode = templateCode,
            Imports = RazorFacts.DefaultNamespaces.Select(ns => new RazorFragment() { Text = ns }).ToList(),
        };
    }

    private RazorProject CreateRazorProject()
    {
        RazorProject project = new RazorProject()
        {
            RootNamespace = this.RootNamespace,
            Items = this.GetProjectItems().ToList(),
            ProjectDirectory = Environment.CurrentDirectory,
            IntermediateDirectory = this.IntermediatePath ?? RazorProjectConventions.DefaultIntermediateDirectory,
        };

        if (string.IsNullOrWhiteSpace(project.RootNamespace))
            project.RootNamespace = this.ProjectName;

        if (string.IsNullOrWhiteSpace(project.RootNamespace))
            project.RootNamespace = null;

        return project;
    }

    private IEnumerable<RazorProjectItem> GetProjectItems()
    {
        foreach (ITaskItem taskItem in this.Sources)
        {
            string fullPath = taskItem.GetMetadata("FullPath");
            string specPath = taskItem.ItemSpec;
            string linkPath = taskItem.GetMetadata("Link");

            yield return new RazorProjectItem()
            {
                ProjectPath = string.IsNullOrEmpty(linkPath) ? specPath : linkPath,
                FullPath = fullPath,
            };
        }
    }
}
