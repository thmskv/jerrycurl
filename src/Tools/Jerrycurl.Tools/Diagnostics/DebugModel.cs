using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Jerrycurl.Tools.Diagnostics
{
    public class DebugModel
    {
        public string Message { get; set; }
        public string Log { get; set; }
        public string Type { get; set; }

        internal static JsonSerializerOptions Options { get; } = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            ReadCommentHandling = JsonCommentHandling.Skip,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public static async Task<DebugModel> FromFileAsync(string path)
        {
            using var stream = File.OpenRead(path);

            var model = await JsonSerializer.DeserializeAsync<DebugModel>(stream, Options);

            return model;
        }

        public async Task ToFileAsync(string path)
        {
            using FileStream stream = File.OpenWrite(path);

            await JsonSerializer.SerializeAsync(stream, this, Options);
        }
    }
}
