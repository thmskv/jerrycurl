using System.Text.Json;

namespace Jerrycurl.Cqs.Metadata.Json;

internal class JsonBindingHelperContract : BindingHelperContract<JsonSerializerOptions>
{
    public JsonBindingHelperContract(JsonSerializerOptions options)
        : base(options)
    {

    }
}
