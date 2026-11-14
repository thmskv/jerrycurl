using System.Text.Json.Nodes;
using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Vendors.Postgres.Test.Models;

[Table("jerry_jsonnodes")]
public class JsonNodeModel
{
    public int Id { get; set; }
    public JsonNode Document { get; set; }
    public JsonNode NullDocument { get; set; }
    public JsonObject Object { get; set; }
    public JsonArray Array { get; set; }
}
