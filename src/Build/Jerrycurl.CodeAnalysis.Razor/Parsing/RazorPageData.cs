using System.Collections.Generic;
using Jerrycurl.CodeAnalysis.Razor.Parsing.Directives;

namespace Jerrycurl.CodeAnalysis.Razor.Parsing;

public class RazorPageData
{
    public string SourceName { get; set; }
    public string SourceChecksum { get; set; }

    public IList<RazorFragment> Imports { get; set; } = [];

    public RazorFragment Class { get; set; }
    public RazorFragment Model { get; set; }
    public RazorFragment Result { get; set; }
    public RazorFragment Namespace { get; set; }
    public RazorFragment Template { get; set; }

    public IList<InjectDirective> Projections { get; set; } = [];
    public IList<InjectDirective> Injections { get; set; } = [];
    public IList<RazorFragment> Content { get; set; } = [];
}
