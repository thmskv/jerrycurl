using Jerrycurl.Relations.Internal.Parsing;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.IO
{
    internal abstract class NodeWriter
    {
        public IRelationMetadata Metadata { get; }
        public string NamePart { get; set; }
        public QueueIndex Queue { get; set; }

        public NodeWriter(Node node)
        {
            this.Metadata = node.Metadata;
        }

        public override string ToString() => $"{this.GetType().Name}: {this.Metadata.Identity.Name}";
    }
}
