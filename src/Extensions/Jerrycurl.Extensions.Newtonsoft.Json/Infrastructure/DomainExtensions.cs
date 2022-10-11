using Jerrycurl.Extensions.Newtonsoft.Json.Metadata;
using Newtonsoft.Json;

namespace Jerrycurl.Mvc
{
    public static class DomainExtensions
    {
        public static DomainOptions UseNewtonsoftJson(this DomainOptions options) => options.UseNewtonsoftJson(null);

        public static DomainOptions UseNewtonsoftJson(this DomainOptions options, JsonSerializerSettings settings)
        {
            settings ??= JsonConvert.DefaultSettings?.Invoke() ?? new JsonSerializerSettings();

            options.Use(new NewtonsoftJsonBindingContractResolver(settings));
            options.Use(new NewtonsoftJsonContractResolver(settings));

            return options;
        }
    }
}
