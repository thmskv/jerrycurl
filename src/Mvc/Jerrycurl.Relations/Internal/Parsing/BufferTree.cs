using System.Collections.Generic;
using Jerrycurl.Relations.Internal.IO;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Parsing
{
    internal class BufferTree
    {
        public DotNotation Notation { get; set; }
        public SourceReader Source { get; set; }
        public List<QueueReader> Queues { get; } = new List<QueueReader>();
        public List<FieldWriter> Fields { get; } = new List<FieldWriter>();
        public IRelationHeader Header { get; set; }
    }
}
