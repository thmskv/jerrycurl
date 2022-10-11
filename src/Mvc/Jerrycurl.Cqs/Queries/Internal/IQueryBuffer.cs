using System.Collections.Generic;
using Jerrycurl.Cqs.Queries.Internal.Caching;

namespace Jerrycurl.Cqs.Queries.Internal
{
    internal interface IQueryBuffer
    {
        internal List<AggregateAttribute> AggregateHeader { get; }
        internal ElasticArray ListData { get; }
        internal ElasticArray AggregateData { get; }
    }
}
