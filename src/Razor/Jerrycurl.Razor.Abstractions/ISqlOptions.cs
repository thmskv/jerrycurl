using Jerrycurl.Cqs.Filters;
using System.Collections.Generic;

namespace Jerrycurl.Razor;

public interface ISqlOptions
{
    public IReadOnlyCollection<IFilter> Filters { get; }

    /// <summary>
    /// The maximum number of bytes each generated ADO.NET command can contain.
    /// </summary>
    public int MaxSql { get; set; }

    /// <summary>
    /// The maximum number of parameters each generated ADO.NET command can contain.
    /// </summary>
    public int MaxParameters { get; set; }
}
