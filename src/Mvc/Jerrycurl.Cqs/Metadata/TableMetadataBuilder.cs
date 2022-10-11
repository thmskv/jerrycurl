using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Jerrycurl.Collections;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata
{
    public class TableMetadataBuilder : Collection<ITableContractResolver>, ITableMetadataBuilder
    {
        public ITableContractResolver DefaultResolver { get; set; } = new DefaultTableContractResolver();

        public ITableMetadata GetMetadata(IMetadataBuilderContext context) => this.GetMetadata(context, context.Relation);

        private ITableMetadata GetMetadata(IMetadataBuilderContext context, IRelationMetadata relation)
        {
            ITableMetadata parent = context.GetMetadata<ITableMetadata>(relation.Parent.Identity.Name) ?? this.GetMetadata(context, relation.Parent);

            if (parent == null)
                return null;
            else if (parent.Item != null && parent.Item.Identity.Equals(relation.Identity))
                return parent.Item;

            return parent.Properties.FirstOrDefault(m => m.Identity.Equals(relation.Identity));
        }

        public void Initialize(IMetadataBuilderContext context) => this.CreateMetadata(context, context.Relation, null);

        private IEnumerable<TableMetadata> CreateProperties(IMetadataBuilderContext context, TableMetadata parent)
        {
            foreach (IRelationMetadata property in parent.Relation.Properties)
                yield return this.CreateMetadata(context, property, parent);
        }

        private TableMetadata CreateItem(IMetadataBuilderContext context, TableMetadata parent)
        {
            if (parent.Relation.Item == null)
                return null;

            return this.CreateMetadata(context, parent.Relation.Item, parent);
        }

        private TableMetadata CreateMetadata(IMetadataBuilderContext context, IRelationMetadata relation, TableMetadata parent)
        {
            TableMetadata metadata = new TableMetadata(relation);

            metadata.Item = this.CreateItem(context, metadata);
            
            this.ApplyContracts(metadata, parent);

            metadata.Properties = new Lazy<IReadOnlyList<TableMetadata>>(() => this.CreateProperties(context, metadata).ToList());

            context.AddMetadata<ITableMetadata>(metadata);

            return metadata;
        }

        private void ApplyContracts(TableMetadata metadata, TableMetadata parent)
        {
            IEnumerable<ITableContractResolver> allResolvers = this;

            if (this.DefaultResolver != null)
                allResolvers = new[] { this.DefaultResolver }.Concat(allResolvers);

            foreach (ITableContractResolver resolver in allResolvers.NotNull().OrderBy(r => r.Priority))
            {
                metadata.TableName = resolver.GetTableName(metadata)?.ToList() ?? metadata.TableName;
                metadata.ColumnName = resolver.GetColumnName(metadata) ?? metadata.ColumnName;
            }

            if (metadata.TableName != null)
                metadata.Flags |= TableMetadataFlags.Table;

            if (metadata.ColumnName != null)
                metadata.Flags |= TableMetadataFlags.Column;

            if (metadata.HasFlag(TableMetadataFlags.Column) && parent != null && parent.HasFlag(TableMetadataFlags.Table))
                metadata.Owner = parent;
        }
    }
}
