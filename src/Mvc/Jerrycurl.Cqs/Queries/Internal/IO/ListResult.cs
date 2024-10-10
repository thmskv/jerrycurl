using System.Collections.Generic;
using Jerrycurl.Cqs.Queries.Internal.IO.Targets;
using Jerrycurl.Cqs.Queries.Internal.IO.Writers;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Queries.Internal.IO;

internal class ListResult : BaseResult
{
    public QueryType QueryType { get; }
    public List<TargetWriter> Writers { get; set; } = [];
    public List<ListTarget> Targets { get; set; } = [];
    public List<AggregateWriter> Aggregates { get; set; } = [];

    public ListResult(ISchema schema, QueryType queryType)
        : base(schema)
    {
        this.QueryType = queryType;
    }
}
