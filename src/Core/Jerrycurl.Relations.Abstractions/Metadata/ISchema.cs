namespace Jerrycurl.Relations.Metadata;

public interface ISchema
{
    public IRelationMetadata Model { get; }
    public DotNotation Notation { get; }
    public ISchemaStore Store { get; }

    public TMetadata Lookup<TMetadata>(string name) where TMetadata : IMetadata;
    public TMetadata Lookup<TMetadata>() where TMetadata : IMetadata;
    public IRelationMetadata Lookup(string name);

    public TMetadata Require<TMetadata>(string name) where TMetadata : IMetadata;
    public TMetadata Require<TMetadata>() where TMetadata : IMetadata;
    public IRelationMetadata Require(string name);
}