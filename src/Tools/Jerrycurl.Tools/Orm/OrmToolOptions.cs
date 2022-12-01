using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using Jerrycurl.Tools.Orm.Model.Json;
using System.Text.Json.Serialization;

namespace Jerrycurl.Tools.Orm
{
    public class OrmToolOptions
    {
        public string Name { get; set; }
        public string Vendor { get; set; }
        public string Connection { get; set; }
        [JsonIgnore]
        public string Input { get; set; }
        public string Output { get; set; }
        public string Transform { get; set; }
        public string Namespace { get; set; }
        [JsonIgnore]
        public bool Verbose { get; set; }
        public Dictionary<string, string> Flags { get; set; }
        public Dictionary<string, string> Snippets { get; set; }

        public static async Task<OrmToolOptions> FromFileAsync(string path)
        {
            using var stream = File.OpenRead(path);

            var model = await JsonSerializer.DeserializeAsync<OrmToolOptions>(stream, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
                Converters =
                {
                    new StringConverter(),
                },
            });

            model.Input = path;

            return model;
        }
    }
}
