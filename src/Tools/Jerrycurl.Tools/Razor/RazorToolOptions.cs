using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jerrycurl.Tools.Razor
{
    public class RazorToolOptions
    {
        public string ProjectDirectory { get; set; }
        public string RootNamespace { get; set; }
        public string OutputDirectory { get; set; }
        public bool NoClean { get; set; }
        public bool Verbose { get; set; }
        public List<string> Imports { get; set; } = new List<string>();
        public List<string> Directories { get; set; } = new List<string>();
        public List<string> Files { get; set; } = new List<string>();

        public static async Task<RazorToolOptions> FromFileAsync(string path)
        {
            using var stream = File.OpenRead(path);

            var model = await JsonSerializer.DeserializeAsync<RazorToolOptions>(stream, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                ReadCommentHandling = JsonCommentHandling.Skip,
            });

            return model;
        }
    }
}
