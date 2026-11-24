using System.Collections.Generic;

namespace Jerrycurl.Razor.Projections;

public interface IProjectionValues<TItem> : IEnumerable<IProjection<TItem>>
{
    public ProjectionIdentity Identity { get; }
    public IProcContext Context { get; }
    public IEnumerable<IProjection<TItem>> Items { get; }
}
