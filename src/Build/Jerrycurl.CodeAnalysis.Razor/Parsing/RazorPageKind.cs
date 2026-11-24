namespace Jerrycurl.CodeAnalysis.Razor.Parsing;

public enum RazorPageKind
{
    /// <summary>No <c>@query</c> or <c>@command</c>: the page is only usable through <c>@template</c> or <c>Subquery</c>/<c>Subcommand</c>.</summary>
    Partial = 0,
    Query = 1,
    Command = 2,
}
