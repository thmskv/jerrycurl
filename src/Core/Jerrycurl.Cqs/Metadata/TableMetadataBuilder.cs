using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata;

public class TableMetadataBuilder : ITableMetadataBuilder
{
    public ITableNamingPolicy NamingPolicy { get; set; } = new DefaultTableNamingPolicy();

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
        if (this.NamingPolicy != null)
        {
            metadata.TableName = this.NamingPolicy.GetTableName(metadata)?.ToList();
            metadata.ColumnName = this.NamingPolicy.GetColumnName(metadata);
        }

        if (metadata.TableName != null)
            metadata.Flags |= TableMetadataFlags.Table;

        if (metadata.ColumnName != null)
            metadata.Flags |= TableMetadataFlags.Column;

        if (metadata.HasFlag(TableMetadataFlags.Column) && parent != null && parent.HasFlag(TableMetadataFlags.Table))
            metadata.Owner = parent;
    }
}
