using Jerrycurl.Relations.Internal.Parsing;

namespace Jerrycurl.Relations.Internal.IO
{
    internal class QueueReader : NodeReader
    {
        public QueueReader(Node node)
            : base(node)
        {
            
        }

        public override string ToString() => this.Metadata.Identity.Name;
    }
}
