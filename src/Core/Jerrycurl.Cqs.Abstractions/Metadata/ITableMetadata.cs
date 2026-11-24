using System.Collections.Generic;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata;

public interface ITableMetadata : IMetadata
{
    public MetadataIdentity Identity { get; }
    public TableMetadataFlags Flags { get; }
    public ITableMetadata Owner { get; }
    public IReadOnlyList<ITableMetadata> Properties { get; }
    public ITableMetadata Item { get; }

    public IReadOnlyList<string> TableName { get; }
    public string ColumnName { get; }
}
