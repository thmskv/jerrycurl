using System;
using System.Reflection;
using System.Text.Json;

namespace Jerrycurl.Cqs.Metadata.Json;

public class DefaultJsonNamingPolicy : IJsonNamingPolicy
{
    public JsonSerializerOptions Options { get; }

    public DefaultJsonNamingPolicy(JsonSerializerOptions options)
    {
        this.Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string GetPropertyName(IJsonMetadata metadata)
    {
        PropertyInfo propertyInfo = metadata.Relation.Member as PropertyInfo;

        if (propertyInfo != null)
            return this.Options?.PropertyNamingPolicy?.ConvertName(propertyInfo.Name) ?? propertyInfo.Name;

        return null;
    }
}
