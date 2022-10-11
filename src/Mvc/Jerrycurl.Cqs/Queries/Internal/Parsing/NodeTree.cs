using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Queries.Internal.Parsing
{
    internal class NodeTree
    {
        public ISchema Schema { get; }

        public NodeTree(ISchema schema)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public IList<Node> Nodes { get; } = new List<Node>();
        public IList<Node> Items { get; } = new List<Node>();
        public IList<Node> Data { get; } = new List<Node>();

        public Node FindNode(IBindingMetadata metadata) => this.FindNode(metadata?.Identity);
        public Node FindNode(MetadataIdentity identity) => this.Nodes.FirstOrDefault(n => n.Identity.Equals(identity));
    }
}
