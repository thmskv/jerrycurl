using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Queries.Internal.IO.Readers;
using Jerrycurl.Cqs.Queries.Internal.IO.Targets;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Queries.Internal.IO
{
    internal class AggregateResult : BaseResult
    {
        public BaseReader Value { get; set; }
        public AggregateTarget Target { get; set; }
        public IBindingMetadata Metadata { get; set; }

        public AggregateResult(ISchema schema)
            : base(schema)
        {
            this.Metadata = schema.Require<IBindingMetadata>();
        }
    }
}
