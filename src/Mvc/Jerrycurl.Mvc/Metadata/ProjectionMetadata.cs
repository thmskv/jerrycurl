using System;
using System.Collections.Generic;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Mvc.Metadata
{
    internal class ProjectionMetadata : IProjectionMetadata
    {
        public MetadataIdentity Identity => this.Relation.Identity;
        public Type Type => this.Relation.Type;
        public ITableMetadata Table { get; set; }
        public ITableMetadata Column { get; set; }
        public IReferenceMetadata Reference { get; }
        public IRelationMetadata Relation { get; }
        public Lazy<IReadOnlyList<ProjectionMetadata>> Properties { get; set; }
        public IProjectionMetadata Item { get; set; }
        public IProjectionMetadata List { get; set; }
        public Lazy<IProjectionMetadata> Input { get; set; }
        public IProjectionMetadata Output => this;
        public ProjectionMetadataFlags Flags { get; set; }

        IReadOnlyList<IProjectionMetadata> IProjectionMetadata.Properties => this.Properties.Value;
        IProjectionMetadata IProjectionMetadata.Input => this.Input.Value;

        public ProjectionMetadata(IRelationMetadata relation)
        {
            this.Relation = relation ?? throw new ArgumentNullException(nameof(relation));
            this.Reference = relation.Identity.Lookup<IReferenceMetadata>();
        }

        public override string ToString() => $"IProjectionMetadata: {this.Identity}";
    }
}
