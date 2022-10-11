namespace Jerrycurl.Relations.Metadata
{
    public interface IMetadataBuilderContext
    {
        IRelationMetadata Relation { get; }

        void AddMetadata<TMetadata>(TMetadata metadata) where TMetadata : IMetadata;
        TMetadata GetMetadata<TMetadata>(string name) where TMetadata : IMetadata;
    }
}
