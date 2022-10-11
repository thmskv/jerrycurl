using System.Diagnostics;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Queries.Internal.Parsing;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Queries.Internal.IO.Writers
{
    [DebuggerDisplay("{GetType().Name,nq}: {Metadata.Identity,nq}")]
    internal abstract class BaseWriter
    {
        public IBindingMetadata Metadata { get; protected set; }
        public MetadataIdentity Identity { get; protected set; }

        public BaseWriter()
        {

        }
        public BaseWriter(Node node)
        {
            this.Metadata = node.Metadata;
            this.Identity = node.Identity;
        }

        public BaseWriter(IBindingMetadata metadata)
        {
            this.Metadata = metadata;
            this.Identity = metadata.Identity;
        }
    }
}
