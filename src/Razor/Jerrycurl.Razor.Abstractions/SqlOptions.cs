using Jerrycurl.Cqs.Filters;
using System.Collections.Generic;

namespace Jerrycurl.Razor;

public class SqlOptions : ISqlOptions
{
    public List<IFilter> Filters { get; set; } = [];

    public int MaxSql { get; set; }
    public int MaxParameters { get; set; }

    IReadOnlyCollection<IFilter> ISqlOptions.Filters => this.Filters;
}
