using Jerrycurl.CodeAnalysis.Lexing;

namespace Jerrycurl.CodeAnalysis.Razor.Parsing;

public enum RazorDiagnosticSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2,
}

public class RazorDiagnostic
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public RazorDiagnosticSeverity Severity { get; set; }
    public string SourceName { get; set; }
    public SourceSpan? Span { get; set; }

    public override string ToString() => $"{this.SourceName}({this.Span?.Line ?? 1},{this.Span?.Column ?? 1}): {this.Severity.ToString().ToLowerInvariant()} {this.Id}: {this.Message}";
}

/// <summary>
/// The diagnostics reported for <c>.cssql</c> pages.
/// </summary>
public static class RazorDiagnostics
{
    public const string BothKinds = "JC0001";
    public const string OutputOnCommand = "JC0002";
    public const string KindInImports = "JC0003";
    public const string InvalidSyntax = "JC0004";
    public const string InvalidVisibility = "JC0005";
    public const string NameClash = "JC0006";
    public const string PartialPage = "JC0100";

    public static RazorDiagnostic Create(string id, string title, string message, RazorDiagnosticSeverity severity, RazorFragment at, string fallbackSource = null) => new RazorDiagnostic()
    {
        Id = id,
        Title = title,
        Message = message,
        Severity = severity,
        SourceName = at?.SourceName ?? fallbackSource,
        Span = at?.Span,
    };

    public static RazorDiagnostic Error(string id, string title, string message, RazorFragment at, string fallbackSource = null)
        => Create(id, title, message, RazorDiagnosticSeverity.Error, at, fallbackSource);
}
