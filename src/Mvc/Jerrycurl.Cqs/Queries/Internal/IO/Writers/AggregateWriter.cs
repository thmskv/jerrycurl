using Jerrycurl.Cqs.Queries.Internal.Caching;
using Jerrycurl.Cqs.Queries.Internal.IO.Readers;
using Jerrycurl.Cqs.Queries.Internal.Parsing;

namespace Jerrycurl.Cqs.Queries.Internal.IO.Writers
{
    internal class AggregateWriter : BaseWriter
    {
        public AggregateWriter(Node node)
            : base(node)
        {

        }

        public AggregateAttribute Attribute { get; set; }
        public DataReader Value { get; set; }
    }
}
