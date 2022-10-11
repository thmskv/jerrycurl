using System.Collections.Generic;
using Jerrycurl.Cqs.Queries.Internal;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues
{
    internal class RelationQueueItem<TList> : NameBuffer
    {
        public TList List { get; }
        public List<FieldArray> Cache { get; } = new List<FieldArray>();

        public RelationQueueItem(TList list, string namePart, DotNotation notation)
            : base(namePart, notation)
        {
            this.List = list;
        }
    }
}
