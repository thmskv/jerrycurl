using Jerrycurl.Relations.Internal.Parsing;

namespace Jerrycurl.Relations.Internal.IO
{
    internal class FieldWriter : NodeWriter
    {
        public int[] BufferIndex { get; set; }
        public int? CacheIndex { get; set; }

        public FieldWriter(Node node)
            : base(node)
        {
            
        }
    }
}
