using System.Collections.Generic;
using System.Linq.Expressions;
using Jerrycurl.Relations.Internal.Queues;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.IO
{
    internal class QueueIndex
    {
        public int Buffer { get; set; }
        public IRelationMetadata List { get; set; }
        public IRelationMetadata Item { get; set; }
        public ParameterExpression Variable { get; set; }
        public RelationQueueType Type { get; set; }
        public List<CacheWriter> Cache { get; } = new List<CacheWriter>();
    }
}
