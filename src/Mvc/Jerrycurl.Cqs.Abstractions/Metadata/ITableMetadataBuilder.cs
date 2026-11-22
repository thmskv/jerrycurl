using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata;

public interface ITableMetadataBuilder : IMetadataBuilder<ITableMetadata>
{
    ITableNamingPolicy NamingPolicy { get; set; }
}
