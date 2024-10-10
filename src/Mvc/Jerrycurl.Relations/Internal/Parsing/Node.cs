using System;
using System.Collections.Generic;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Parsing;

internal class Node
{
    public Node(IRelationMetadata metadata)
    {
        this.Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    public MetadataIdentity Identity => this.Metadata.Identity;
    public IRelationMetadata Metadata { get; }
    public IList<Node> Properties { get; } = [];
    public Node Item { get; set; }
    public IList<int> Index { get; } = [];

    public override string ToString() => this.Identity.ToString();
}
