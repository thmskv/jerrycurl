using Jerrycurl.Relations.Metadata;
using System;

namespace Jerrycurl.Relations.Test.Metadata
{
    public class CustomMetadata : IMetadata
    {
        public IRelationMetadata Relation { get; }

        public CustomMetadata(IRelationMetadata relation)
        {
            this.Relation = relation ?? throw new ArgumentNullException(nameof(relation));
        }
    }
}
