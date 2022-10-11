using Jerrycurl.Cqs.Queries.Internal.Caching;
using Jerrycurl.Cqs.Queries.Internal.Parsing;

namespace Jerrycurl.Cqs.Queries.Internal.IO.Readers
{
    internal class AggregateReader : DataReader
    {
        public AggregateReader(Node node)
            : base(node)
        {

        }

        public AggregateAttribute Attribute { get; set; }
        public BaseReader Value { get; set; }
    }
}
