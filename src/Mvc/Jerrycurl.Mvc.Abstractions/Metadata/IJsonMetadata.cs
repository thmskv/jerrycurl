using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Mvc.Metadata
{
    public interface IJsonMetadata : IMetadata
    {
        MetadataIdentity Identity { get; }
        string Path { get; }
        bool IsRoot { get; }
        IJsonMetadata MemberOf { get; }
    }
}
