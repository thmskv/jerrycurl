using System.Collections.Generic;
using Jerrycurl.CodeAnalysis.Razor.Parsing.Directives;

namespace Jerrycurl.CodeAnalysis.Razor.Parsing;

public class RazorPageData
{
    public string SourceName { get; set; }
    public string SourceChecksum { get; set; }

    public IList<RazorFragment> Imports { get; set; } = [];

    public RazorFragment Class { get; set; }
    public RazorFragment Input { get; set; }
    public RazorFragment Output { get; set; }
    public RazorFragment Model { get; set; }
    public RazorFragment Result { get; set; }
    public RazorFragment Namespace { get; set; }
    public RazorFragment Template { get; set; }

    /// <summary>The <c>@query</c> directive, if present. Its text is any (invalid) argument given to it.</summary>
    public RazorFragment Query { get; set; }
    /// <summary>The <c>@command</c> directive, if present. Its text is any (invalid) argument given to it.</summary>
    public RazorFragment Command { get; set; }
    /// <summary>The <c>@visibility</c> directive, if present (either on the page or inherited from <c>_imports.cssql</c>).</summary>
    public RazorFragment Visibility { get; set; }

    public RazorPageKind Kind => this.Query != null ? RazorPageKind.Query : this.Command != null ? RazorPageKind.Command : RazorPageKind.Partial;

    public IList<RazorDiagnostic> Diagnostics { get; set; } = [];

    public IList<InjectDirective> Projections { get; set; } = [];
    public IList<InjectDirective> Injections { get; set; } = [];
    public IList<RazorFragment> Content { get; set; } = [];
}
