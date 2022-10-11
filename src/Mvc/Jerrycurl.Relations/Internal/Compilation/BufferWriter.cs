using System;

namespace Jerrycurl.Relations.Internal.Compilation
{
    internal class BufferWriter
    {
        public Action<RelationBuffer> Initializer { get; set; }
        public Action<RelationBuffer>[] Queues { get; set; }
    }
}
