using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Relations.Metadata
{
    public class SchemaStore : ISchemaStore
    {
        private readonly ConcurrentDictionary<Type, ISchema> entries = new ConcurrentDictionary<Type, ISchema>();
        private readonly List<IMetadataBuilder> builders = new List<IMetadataBuilder>();

        public DotNotation Notation { get; }
        internal RelationMetadataBuilder RelationBuilder { get; } = new RelationMetadataBuilder();

        public IEnumerable<IMetadataBuilder> Builders => new IMetadataBuilder[] { this.RelationBuilder }.Concat(this.builders);

        public SchemaStore()
            : this(new DotNotation())
        {

        }

        public SchemaStore(DotNotation notation)
        {
            this.Notation = notation ?? throw new ArgumentNullException(nameof(notation));
        }

        public SchemaStore(DotNotation notation, params IMetadataBuilder[] builders)
            : this(notation, (IEnumerable<IMetadataBuilder>)builders)
        {

        }

        public SchemaStore(params IMetadataBuilder[] builders)
            : this(new DotNotation(), builders)
        {

        }

        public SchemaStore(IEnumerable<IMetadataBuilder> builders)
            : this(new DotNotation(), builders)
        {

        }

        public SchemaStore(DotNotation notation, IEnumerable<IMetadataBuilder> builders)
            : this(notation)
        {
            this.builders.AddRange(builders ?? Array.Empty<IMetadataBuilder>());
        }

        public ISchema GetSchema(Type modelType)
        {
            if (modelType == null)
                throw new ArgumentNullException(nameof(modelType));

            return this.entries.GetOrAdd(modelType, this.CreateSchema);
        }

        private Schema CreateSchema(Type modelType)
        {
            Schema schema = new Schema(this, modelType);

            schema.Initialize();

            return schema;
        }
    }
}
