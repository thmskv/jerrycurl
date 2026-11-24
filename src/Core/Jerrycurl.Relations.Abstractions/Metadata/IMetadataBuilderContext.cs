namespace Jerrycurl.Relations.Metadata;

public interface IMetadataBuilderContext
{
    public IRelationMetadata Relation { get; }

    public void AddMetadata<TMetadata>(TMetadata metadata) where TMetadata : IMetadata;
    public TMetadata GetMetadata<TMetadata>(string name) where TMetadata : IMetadata;
}
