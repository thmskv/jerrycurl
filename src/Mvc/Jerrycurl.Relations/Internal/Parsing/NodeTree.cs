using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Parsing
{
    internal class NodeTree
    {
        public Node Source { get; set; }
        public IList<Node> Nodes { get; } = new List<Node>();
        public IList<IRelationMetadata> Unreachable { get; } = new List<IRelationMetadata>();

        public Node FindNode(IRelationMetadata metadata) => this.FindNode(metadata?.Identity);
        public Node FindNode(MetadataIdentity identity) => this.Nodes.FirstOrDefault(n => n.Identity.Equals(identity));
    }
}
