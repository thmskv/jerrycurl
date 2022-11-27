﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using Jerrycurl.Tools.Orm.Model.Json;

namespace Jerrycurl.Tools.Orm
{
    public class OrmToolOptions
    {
        public string Name { get; set; }
        public string Vendor { get; set; }
        public string Connection { get; set; }
        public string Input { get; set; }
        public string Output { get; set; }
        public string Transform { get; set; }
        public string Namespace { get; set; }
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