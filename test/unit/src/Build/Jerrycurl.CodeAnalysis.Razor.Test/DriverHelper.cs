using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Jerrycurl.CodeAnalysis.Razor.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Jerrycurl.CodeAnalysis.Razor.Test;

/// <summary>
/// Runs <see cref="RazorSourceGenerator"/> through Roslyn's generator driver, as a real build would.
/// </summary>
internal static class DriverHelper
{
    public static GeneratorDriverRunResult Run(IDictionary<string, string> properties, params (string Path, string Content)[] files)
    {
        CSharpCompilation compilation = CSharpCompilation.Create("Driver", [], [], new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Dictionary<string, string> globals = new Dictionary<string, string>()
        {
            ["build_property.ProjectDir"] = GeneratorHelper.ProjectDirectory + Path.DirectorySeparatorChar,
            ["build_property.RootNamespace"] = "TestApp",
        };

        foreach (var (key, value) in properties ?? new Dictionary<string, string>())
            globals["build_property." + key] = value;

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new RazorSourceGenerator().AsSourceGenerator()],
            additionalTexts: [.. files.Select(f => (AdditionalText)new InMemoryText(Path.Combine(GeneratorHelper.ProjectDirectory, f.Path), f.Content))],
            optionsProvider: new OptionsProvider(globals));

        return driver.RunGenerators(compilation).GetRunResult();
    }

    public static GeneratorDriverRunResult Run(params (string Path, string Content)[] files) => Run(null, files);

    private sealed class InMemoryText(string path, string content) : AdditionalText
    {
        public override string Path { get; } = path;
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(content, System.Text.Encoding.UTF8);
    }

    private sealed class OptionsProvider(Dictionary<string, string> globals) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(globals);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new Options([]);
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new Options([]);
    }

    private sealed class Options(Dictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value) => values.TryGetValue(key, out value);
        public override IEnumerable<string> Keys => values.Keys;
    }
}
