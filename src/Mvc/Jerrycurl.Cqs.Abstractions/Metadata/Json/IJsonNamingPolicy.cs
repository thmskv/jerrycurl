namespace Jerrycurl.Cqs.Metadata.Json;

public interface IJsonNamingPolicy
{
    string GetPropertyName(IJsonMetadata metadata);
}
