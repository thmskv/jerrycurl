using Jerrycurl.Relations.Internal.Parsing;

namespace Jerrycurl.Relations.Internal.IO
{
    internal class CacheWriter : NodeWriter
    {
        public int BufferIndex { get; set; }
        public int CacheIndex { get; set; }

        public CacheWriter(Node node)
            : base(node)
        {
            
        }
    }
}
