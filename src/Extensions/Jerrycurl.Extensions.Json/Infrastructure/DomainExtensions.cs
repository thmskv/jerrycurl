using System.Text.Json;
using Jerrycurl.Extensions.Json.Metadata;

namespace Jerrycurl.Mvc
{
    public static class DomainExtensions
    {
        public static DomainOptions UseJson(this DomainOptions options) => options.UseJson(null);

        public static DomainOptions UseJson(this DomainOptions options, JsonSerializerOptions serializerOptions)
        {
            serializerOptions ??= new JsonSerializerOptions();

            options.Use(new JsonBindingContractResolver(serializerOptions));
            options.Use(new JsonContractResolver(serializerOptions));

            return options;
        }
    }
}
