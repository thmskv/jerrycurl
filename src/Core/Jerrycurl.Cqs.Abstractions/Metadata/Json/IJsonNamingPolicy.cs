namespace Jerrycurl.Cqs.Metadata.Json;

public interface IJsonNamingPolicy
{
    public string GetPropertyName(IJsonMetadata metadata);
}
