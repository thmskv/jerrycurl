using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata
{
    internal class ReferenceMetadata : IReferenceMetadata
    {
        public MetadataIdentity Identity => this.Relation.Identity;
        public IRelationMetadata Relation { get; }
        public Type Type => this.Relation.Type;

        public ReferenceMetadataFlags Flags { get; set; }
        public Lazy<IReadOnlyList<ReferenceKey>> Keys { get; set; }
        public Lazy<IReadOnlyList<Reference>> References { get; set; }
        public Lazy<IReadOnlyList<ReferenceMetadata>> Properties { get; set; }
        public ReferenceMetadata Item { get; set; }
        public IReadOnlyList<Attribute> Annotations => this.Relation.Annotations;

        public Lazy<IReadOnlyList<Reference>> ParentReferences { get; set; }
        public Lazy<IReadOnlyList<Reference>> ChildReferences { get; set; }

        public ReferenceMetadata Parent { get; set; }

        IReadOnlyList<IReferenceMetadata> IReferenceMetadata.Properties => this.Properties?.Value;
        IReferenceMetadata IReferenceMetadata.Item => this.Item;
        IReadOnlyList<IReference> IReferenceMetadata.References => this.ParentReferences.Value.Concat(this.ChildReferences.Value).ToList();
        IReadOnlyList<IReferenceKey> IReferenceMetadata.Keys => this.Keys?.Value;

        public ReferenceMetadata(IRelationMetadata relation)
        {
            this.Relation = relation ?? throw new ArgumentNullException(nameof(relation));
        }

        public override string ToString() => $"IReferenceMetadata: {this.Identity}";

    }
}