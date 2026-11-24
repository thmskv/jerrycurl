namespace Jerrycurl.Relations.Metadata;

public interface IMetadataBuilder
{
    public void Initialize(IMetadataBuilderContext context);
}

public interface IMetadataBuilder<TMetadata> : IMetadataBuilder
     where TMetadata : IMetadata
{
    public TMetadata GetMetadata(IMetadataBuilderContext context);
}
