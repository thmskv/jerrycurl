using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using Jerrycurl.Collections;
using Jerrycurl.Reflection;

namespace Jerrycurl.Relations.Metadata
{
    internal class Schema : ISchema
    {
        private readonly ConcurrentDictionary<MetadataKey, object> entries = new ConcurrentDictionary<MetadataKey, object>();
        private readonly ConcurrentDictionary<Type, ReaderWriterLockSlim> locks = new ConcurrentDictionary<Type, ReaderWriterLockSlim>();
        private readonly Type modelType;

        public IRelationMetadata Model { get; private set; }
        public SchemaStore Store { get; }
        public DotNotation Notation => this.Store.Notation;

        ISchemaStore ISchema.Store => this.Store;

        public Schema(SchemaStore store, Type modelType)
        {
            this.Store = store ?? throw new ArgumentNullException(nameof(store));
            this.modelType = modelType ?? throw new ArgumentNullException(nameof(modelType));
        }

        public void Initialize()
        {
            this.Store.RelationBuilder.Initialize(this, this.modelType);

            this.Model = this.GetCachedMetadata<IRelationMetadata>(this.Notation.Model());

            foreach (IMetadataBuilder builder in this.Store.Builders)
            {
                MetadataBuilderContext context = new MetadataBuilderContext(this, this.Model);

                builder.Initialize(context);
            }
        }

        public void AddMetadata<TMetadata>(TMetadata metadata)
            where TMetadata : IMetadata
        {
            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            if (metadata.Relation == null)
                throw new ArgumentNullException(nameof(metadata.Relation));

            MetadataKey key = MetadataKey.FromIdentity<TMetadata>(metadata.Relation.Identity);

            if (!this.entries.TryAdd(key, metadata))
                throw new InvalidOperationException("Metadata already added.");
        }

        internal TMetadata GetCachedMetadata<TMetadata>(string name)
            where TMetadata : IMetadata
        {
            return (TMetadata)this.entries.GetValueOrDefault(this.CreateKey<TMetadata>(name));
        }

        private MetadataKey CreateKey<TMetadata>(string name) => new MetadataKey(typeof(TMetadata), name, this.Notation.Comparer);
        private ReaderWriterLockSlim GetLock<TMetadata>() => this.locks.GetOrAdd(typeof(TMetadata), _ => new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion));
        private void RemoveLock<TMetadata>() => this.locks.TryRemove(typeof(TMetadata), out _);

        public TMetadata Lookup<TMetadata>(string name)
            where TMetadata : IMetadata
        {
            MetadataKey key = this.CreateKey<TMetadata>(name);
            ReaderWriterLockSlim slim = this.GetLock<TMetadata>();

            try
            {
                slim.EnterReadLock();

                if (this.entries.TryGetValue(key, out object value))
                    return (TMetadata)value;
            }
            catch (LockRecursionException ex)
            {
                throw MetadataBuilderException.NoRecursion<TMetadata>(name, ex);
            }
            finally
            {
                if (slim.IsReadLockHeld)
                    slim.ExitReadLock();
            }


            slim.EnterWriteLock();

            try
            {
                MetadataIdentity identity = new MetadataIdentity(this, name);
                IRelationMetadata relation = this.GetCachedMetadata<IRelationMetadata>(name) ?? this.Store.RelationBuilder.GetMetadata(this, identity);

                if (relation == null)
                    return default;

                foreach (IMetadataBuilder<TMetadata> metadataBuilder in this.Store.Builders.OfType<IMetadataBuilder<TMetadata>>())
                {
                    MetadataBuilderContext context = new MetadataBuilderContext(this, relation);

                    TMetadata metadata = metadataBuilder.GetMetadata(context);

                    if (metadata != null)
                        return metadata;
                }

                return default;
            }
            finally
            {
                if (slim.IsWriteLockHeld)
                    slim.ExitWriteLock();

                this.RemoveLock<TMetadata>();
            }
        }

        public override string ToString() => this.Model.Type.GetSanitizedName();

        public IRelationMetadata Lookup(string name) => this.Lookup<IRelationMetadata>(name);
        public TMetadata Lookup<TMetadata>() where TMetadata : IMetadata
            => this.Lookup<TMetadata>(this.Notation.Model());

        public IRelationMetadata Require(string name) => this.Require<IRelationMetadata>(name);
        public TMetadata Require<TMetadata>(string name)
            where TMetadata : IMetadata
            => this.Lookup<TMetadata>(name) ?? throw MetadataException.NotFound<TMetadata>(this, name);

        public TMetadata Require<TMetadata>() where TMetadata : IMetadata
            => this.Require<TMetadata>(this.Notation.Model());
    }
}
