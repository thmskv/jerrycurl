using System.Collections.Generic;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Queries.Internal.IO.Targets;

namespace Jerrycurl.Cqs.Queries.Internal.IO.Readers
{
    internal class NewReader : BaseReader
    {
        public KeyReader PrimaryKey { get; set; }
        public IList<JoinTarget> Joins { get; } = new List<JoinTarget>();
        public IList<BaseReader> Properties { get; set; } = new List<BaseReader>();

        public NewReader(IBindingMetadata metadata)
        {
            this.Metadata = metadata;
            this.Identity = metadata.Identity;
        }

        public NewReader(IReferenceMetadata metadata)
            : this(metadata.Identity.Require<IBindingMetadata>())
        {

        }
    }
}
