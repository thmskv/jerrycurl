using System;
using System.Collections.Generic;
using System.Reflection;

namespace Jerrycurl.Relations.Metadata
{
    internal class RelationMetadata : IRelationMetadata
    {
        public Schema Schema { get; }
        public DotNotation Notation { get; }
        public MetadataIdentity Identity { get; }

        public IRelationMetadata Parent { get; set; }
        public RelationMetadata Owner { get; set; }
        public RelationMetadata Item { get; set; }
        public Lazy<IReadOnlyList<RelationMetadata>> Properties { get; set; }
        public Lazy<IRelationMetadata> Recursor { get; set; }
        public RelationMetadataFlags Flags { get; set; }
        public int Depth { get; set; }
        public IRelationMetadata Relation => this;

        public IReadOnlyList<Attribute> Annotations { get; set; } = Array.Empty<Attribute>();
        public MemberInfo Member { get; set; }
        public Type Type { get; set; }
        public MethodInfo ReadIndex { get; set; }
        public MethodInfo WriteIndex { get; set; }

        IReadOnlyList<IRelationMetadata> IRelationMetadata.Properties => this.Properties.Value;
        IRelationMetadata IRelationMetadata.Item => this.Item;
        IRelationMetadata IRelationMetadata.Recursor => this.Recursor?.Value;
        IRelationMetadata IRelationMetadata.Owner => this.Owner;
        ISchema IRelationMetadata.Schema => this.Schema;

        public RelationMetadata(Schema schema, MetadataIdentity identity)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(identity));
            this.Notation = schema.Notation;
            this.Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        public override string ToString() => $"IRelationMetadata: {this.Identity}";
    }
}
