using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Queries.Internal.Extensions;
using Jerrycurl.Cqs.Queries.Internal.IO.Targets;

namespace Jerrycurl.Cqs.Queries.Internal.IO.Readers
{
    internal class JoinReader : BaseReader
    {
        public JoinTarget Target { get; set; }

        public JoinReader(IReference reference)
        {
            IReferenceMetadata metadata = reference.List ?? reference.Find(ReferenceFlags.Child).Metadata;

            this.Metadata = metadata.Identity.Require<IBindingMetadata>();
            this.Identity = this.Metadata.Identity;
        }
    }
}
