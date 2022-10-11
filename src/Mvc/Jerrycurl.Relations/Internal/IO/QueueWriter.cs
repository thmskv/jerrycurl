using Jerrycurl.Relations.Internal.Parsing;

namespace Jerrycurl.Relations.Internal.IO
{
    internal class QueueWriter : NodeWriter
    {
        public QueueIndex Next { get; set; }

        public QueueWriter(Node node)
            : base(node)
        {

        }
    }
}
