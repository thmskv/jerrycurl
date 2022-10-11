using Jerrycurl.Diagnostics;
using Jerrycurl.Relations.Metadata;
using System;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Relations.Internal.Caching
{
    internal class RelationCacheKey : IEquatable<RelationCacheKey>
    {
        public IRelationHeader Header { get; }
        public MetadataIdentity Source { get; }

        public RelationCacheKey(MetadataIdentity source, IRelationHeader header)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.Header = header ?? throw new ArgumentNullException(nameof(header));
        }

        public bool Equals(RelationCacheKey other) => Equality.Combine(this, other, m => m.Source, m => m.Header);
        public override int GetHashCode() => HashCode.Combine(this.Source, this.Header);
        public override bool Equals(object obj) => (obj is RelationCacheKey other && this.Equals(other));

    }
}
