using System.Collections.Generic;
using Jerrycurl.CodeAnalysis.Razor.Parsing;

namespace Jerrycurl.CodeAnalysis.Razor.ProjectSystem.Conventions;

public interface IRazorProjectConvention
{
    public void Apply(RazorProject project, IList<RazorPage> result);
}
