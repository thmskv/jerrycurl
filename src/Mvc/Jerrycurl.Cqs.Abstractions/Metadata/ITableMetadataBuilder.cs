using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata;

public interface ITableMetadataBuilder : IMetadataBuilder<ITableMetadata>
{
    public ITableNamingPolicy NamingPolicy { get; set; }
}
