using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata.Json;

public interface IJsonMetadata : IMetadata
{
    public MetadataIdentity Identity { get; }
    public string Path { get; }
    public bool IsRoot { get; }
    public IJsonMetadata MemberOf { get; }
}
