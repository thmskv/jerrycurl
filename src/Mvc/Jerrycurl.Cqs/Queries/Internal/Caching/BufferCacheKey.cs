using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Diagnostics;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Cqs.Queries.Internal.Caching
{
    internal class BufferCacheKey : IEquatable<BufferCacheKey>
    {
        public MetadataIdentity Target { get; set; }
        public IReadOnlyList<Type> Key { get; set; }

        public BufferCacheKey(IEnumerable<Type> key)
        {
            this.Key = key?.ToList() ?? throw new ArgumentNullException(nameof(key));
        }

        public BufferCacheKey(MetadataIdentity target)
        {
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public BufferCacheKey(MetadataIdentity target, IEnumerable<Type> key)
        {
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
            this.Key = key?.ToList() ?? throw new ArgumentNullException(nameof(key));
        }

        public bool Equals(BufferCacheKey other)
        {
            if (other == null)
                return false;

            Equality eq = new Equality();

            eq.Add(this.Target, other?.Target);
            eq.AddRange(this.Key, other?.Key);

            return eq.ToEquals();
        }
        public override bool Equals(object obj) => (obj is BufferCacheKey other && this.Equals(other));
        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();

            hashCode.Add(this.Target);

            if (this.Key != null)
                hashCode.AddRange(this.Key);

            return hashCode.ToHashCode();
        }
    }
}
