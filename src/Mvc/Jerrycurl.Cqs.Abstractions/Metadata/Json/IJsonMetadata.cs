using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata.Json;

public interface IJsonMetadata : IMetadata
{
    MetadataIdentity Identity { get; }
    string Path { get; }
    bool IsRoot { get; }
    IJsonMetadata MemberOf { get; }
}
