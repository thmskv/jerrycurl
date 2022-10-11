using Jerrycurl.Cqs.Metadata;
using Newtonsoft.Json;

namespace Jerrycurl.Extensions.Newtonsoft.Json.Metadata
{
    internal class NewtonsoftJsonBindingHelperContract : BindingHelperContract<JsonSerializerSettings>
    {
        public NewtonsoftJsonBindingHelperContract(JsonSerializerSettings settings)
            : base(settings)
        {
            
        }
    }
}
