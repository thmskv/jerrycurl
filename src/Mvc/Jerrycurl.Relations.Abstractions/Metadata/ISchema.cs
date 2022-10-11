namespace Jerrycurl.Relations.Metadata
{
    public interface ISchema
    {
        IRelationMetadata Model { get; }
        DotNotation Notation { get; }
        ISchemaStore Store { get; }

        TMetadata Lookup<TMetadata>(string name) where TMetadata : IMetadata;
        TMetadata Lookup<TMetadata>() where TMetadata : IMetadata;
        IRelationMetadata Lookup(string name);

        TMetadata Require<TMetadata>(string name) where TMetadata : IMetadata;
        TMetadata Require<TMetadata>() where TMetadata : IMetadata;
        IRelationMetadata Require(string name);
    }
}