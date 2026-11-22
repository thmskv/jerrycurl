namespace Jerrycurl.Mvc.Metadata;

public interface IJsonNamingPolicy
{
    string GetPropertyName(IJsonMetadata metadata);
}
