using System;
using System.Collections.Generic;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata
{
    internal class TableMetadata : ITableMetadata
    {
        public MetadataIdentity Identity => this.Relation.Identity;
        public IRelationMetadata Relation { get; }

        public TableMetadataFlags Flags { get; set; }
        public TableMetadata Owner { get; set; }
        public IReadOnlyList<string> TableName { get; set; }
        public Lazy<IReadOnlyList<TableMetadata>> Properties { get; set; }
        public ITableMetadata Item { get; set; }
        public string ColumnName { get; set; }

        IReadOnlyList<ITableMetadata> ITableMetadata.Properties => this.Properties?.Value;
        ITableMetadata ITableMetadata.Owner => this.Owner;

        public TableMetadata(IRelationMetadata relation)
        {
            this.Relation = relation ?? throw new ArgumentNullException(nameof(relation));
        }

        public override string ToString() => $"ITableMetadata: {this.Identity}";
    }
}
