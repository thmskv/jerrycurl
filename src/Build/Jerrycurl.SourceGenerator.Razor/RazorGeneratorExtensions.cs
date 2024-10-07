using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Jerrycurl.SourceGenerator.Razor;

internal static class RazorGeneratorExtensions
{
    private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

    public static string GetMSBuildProperty(
        this GeneratorExecutionContext context,
        string name,
        string defaultValue = "")
    {
        context.AnalyzerConfigOptions.GlobalOptions.TryGetValue($"build_property.{name}", out var value);
        return value ?? defaultValue;
    }

    public static string[] GetMSBuildItems(this GeneratorExecutionContext context, string name)
        => context
            .AdditionalFiles
            .Where(f => context.AnalyzerConfigOptions
                .GetOptions(f)
                .TryGetValue(SourceItemGroupMetadata, out var sourceItemGroup)
                && sourceItemGroup == name)
            .Select(f => f.Path)
            .ToArray();
}
