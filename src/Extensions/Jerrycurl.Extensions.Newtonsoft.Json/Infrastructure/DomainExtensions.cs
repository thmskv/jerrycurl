using Jerrycurl.Collections;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Metadata.Json;
using Jerrycurl.Extensions.Newtonsoft.Json.Metadata;
using Newtonsoft.Json;

namespace Jerrycurl.Razor;

public static class DomainExtensions
{
    public static DomainOptions UseNewtonsoftJson(this DomainOptions options) => options.UseNewtonsoftJson(null);

    public static DomainOptions UseNewtonsoftJson(this DomainOptions options, JsonSerializerSettings settings)
    {
        settings ??= JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings();

        options.Use(new NewtonsoftJsonBindingConvention(settings));
        options.Use(new NewtonsoftJsonNamingPolicy(settings));

        return options;
    }
}
