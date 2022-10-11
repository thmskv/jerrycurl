using Jerrycurl.Relations.Internal.Queues;

namespace Jerrycurl.Relations.Internal.Compilation
{
    internal class RelationBuffer
    {
        public BufferWriter Writer { get; set; }
        public IField Model { get; set; }
        public IField Source { get; set; }
        public IRelationQueue[] Queues { get; set; }
        public IField[] Fields { get; set; }
    }
}
