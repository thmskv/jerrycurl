using System.Text.Json;
using Jerrycurl.Cqs.Metadata;

namespace Jerrycurl.Extensions.Json.Metadata
{
    internal class JsonBindingHelperContract : BindingHelperContract<JsonSerializerOptions>
    {
        public JsonBindingHelperContract(JsonSerializerOptions options)
            : base(options)
        {

        }
    }
}
