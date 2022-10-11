using System;
using System.Collections.Generic;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Queries.Internal.Caching;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Queries.Internal.Parsing
{
    internal class Node
    {
        public Node(MetadataIdentity identity)
        {
            this.Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.Metadata = identity.Require<IBindingMetadata>();
            this.Depth = identity.Schema.Notation.Depth(identity.Name);
        }

        public Node(IBindingMetadata metadata)
        {
            this.Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            this.Identity = metadata.Identity;
            this.Depth = metadata.Identity.Schema.Notation.Depth(metadata.Identity.Name);
        }

        public Node(MetadataIdentity identity, IBindingMetadata metadata)
        {
            this.Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            this.Depth = identity.Schema.Notation.Depth(identity.Name);
        }

        public MetadataIdentity Identity { get; }
        public IBindingMetadata Metadata { get; }
        public DataAttribute Data { get; set; }
        public ISchema Schema => this.Metadata?.Identity.Schema ?? this.Identity?.Schema;
        public IList<Node> Properties { get; } = new List<Node>();
        public bool IsDynamic { get; set; }
        public int Depth { get; }
    }
}
