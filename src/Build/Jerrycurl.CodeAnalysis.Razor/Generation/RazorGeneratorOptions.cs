using System.Collections.Generic;
using Jerrycurl.CodeAnalysis.Razor.Parsing;

namespace Jerrycurl.CodeAnalysis.Razor.Generation;

public class RazorGeneratorOptions
{
    public IList<RazorFragment> Imports { get; set; } = [];
    public RazorFragment Class { get; set; }
    public RazorFragment Namespace { get; set; }

    /// <summary>
    /// Visibility of pages that don't declare <c>@visibility</c> themselves or in an <c>_imports.cssql</c>: <c>internal</c> (default) or <c>public</c>.
    /// </summary>
    public string DefaultVisibility { get; set; } = "internal";

    public string TemplateCode { get; set; }
    public string SourceName { get; set; }
}
