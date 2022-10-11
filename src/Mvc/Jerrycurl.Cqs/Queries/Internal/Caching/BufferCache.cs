using Jerrycurl.Collections;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Queries.Internal.Extensions;
using Jerrycurl.Cqs.Queries.Internal.Parsing;
using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Cqs.Queries.Internal.Caching
{
    internal class BufferCache
    {
        private readonly Dictionary<BufferCacheKey, int> parentMap = new Dictionary<BufferCacheKey, int>();
        private readonly Dictionary<int, Dictionary<MetadataIdentity, int>> childMap = new Dictionary<int, Dictionary<MetadataIdentity, int>>();
        private readonly Dictionary<MetadataIdentity, int> aggregateMap = new Dictionary<MetadataIdentity, int>();
        private readonly object state = new object();

        public ISchema Schema { get; }
        public const int ResultIndex = 0;

        public BufferCache(ISchema schema)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public int GetListIndex(IBindingMetadata metadata, IReference reference)
            => (reference != null ? this.GetListIndex(reference) : this.GetListIndex(metadata));

        public int GetListIndex(IBindingMetadata metadata)
        {
            if (metadata.HasFlag(BindingMetadataFlags.Model))
                return ResultIndex;
            else if (metadata.HasFlag(BindingMetadataFlags.Item) && metadata.Parent.HasFlag(BindingMetadataFlags.Model))
                return ResultIndex;

            return this.GetListIndex(new BufferCacheKey(metadata.Identity));
        }

        public int GetListIndex(IReference reference)
        {
            BufferCacheKey cacheKey = new BufferCacheKey(this.GetParentKeyType(reference));

            return this.GetListIndex(cacheKey);
        }

        public int GetJoinIndex(IReference reference)
        {
            IReference childReference = reference.Find(ReferenceFlags.Child);
            MetadataIdentity target = childReference.Metadata.Identity;

            int parentIndex = this.GetListIndex(reference);

            lock (this.state)
            {
                Dictionary<MetadataIdentity, int> innerMap = this.childMap.GetOrAdd(parentIndex);

                return innerMap.GetOrAdd(target, innerMap.Count);
            }
        }

        public int GetAggregateIndex(MetadataIdentity metadata)
        {
            lock (this.state)
                return this.aggregateMap.GetOrAdd(metadata, this.aggregateMap.Count);
        }

        private IEnumerable<Type> GetParentKeyType(IReference reference)
        {
            IReference parentReference = reference.Find(ReferenceFlags.Parent);

            return parentReference.Key.Properties.Select(m => m.Type.GetKeyType());
        }

        private int GetListIndex(BufferCacheKey key)
        {
            lock (this.state)
                return this.parentMap.GetOrAdd(key, this.parentMap.Count + 1);
        }
    }
}
