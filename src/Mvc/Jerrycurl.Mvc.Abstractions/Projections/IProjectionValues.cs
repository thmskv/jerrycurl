using System.Collections.Generic;

namespace Jerrycurl.Mvc.Projections
{
    public interface IProjectionValues<TItem> : IEnumerable<IProjection<TItem>>
    {
        ProjectionIdentity Identity { get; }
        IProcContext Context { get; }
        IEnumerable<IProjection<TItem>> Items { get; }
    }
}
