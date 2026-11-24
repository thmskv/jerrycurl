using System.Text.Json;
using System.Text.Json.Nodes;
using Jerrycurl.Cqs.Language;
using Jerrycurl.Cqs.Metadata.Annotations;
using Jerrycurl.Cqs.Metadata.Json;
using Jerrycurl.Cqs.Queries;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Extensions.Newtonsoft.Json.Metadata;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Test;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shouldly;

namespace Jerrycurl.Cqs.Test;

public class NewtonsoftJsonTests
{
    public void Test_Newtonsoft_OwnsJsonAttribute()
    {
        var store = GetStore(newtonsoftFirst: false);
        var result = Insert(store, "{ \"renamed\": 12 }", "Payload");

        result.Payload.ShouldNotBeNull();
        result.Payload.Value.ShouldBe(12);
    }

    public void Test_Newtonsoft_OwnsJsonAttribute_RegardlessOfOrder()
    {
        var store = GetStore(newtonsoftFirst: true);
        var result = Insert(store, "{ \"renamed\": 12 }", "Payload");

        result.Payload.Value.ShouldBe(12);
    }

    public void Test_Newtonsoft_SerializesJsonAttributeParameter()
    {
        var store = GetStore(newtonsoftFirst: false);
        var data = new MixedView() { Payload = new Payload() { Value = 12 } };

        BuildParameter(store, data, "Payload").ShouldBe("{\"renamed\":12}");
    }

    public void Test_SystemTextJson_OwnsJsonNode()
    {
        var store = GetStore(newtonsoftFirst: true);
        var result = Insert(store, "{ \"Hello\": \"World!\" }", "Node");

        result.Node.ShouldBeOfType<JsonObject>();
        ((string)result.Node["Hello"]).ShouldBe("World!");
    }

    public void Test_SystemTextJson_OwnsJsonNode_WithJsonAttribute()
    {
        var store = GetStore(newtonsoftFirst: false);
        var result = Insert(store, "[1, 2, 3]", "AttributedNode");

        result.AttributedNode.ShouldBeOfType<JsonArray>().Count.ShouldBe(3);

        var data = new MixedView() { AttributedNode = JsonNode.Parse("{\"a\":1}") };

        BuildParameter(store, data, "AttributedNode").ShouldBe("{\"a\":1}");
    }

    public void Test_Newtonsoft_OwnsJTokenFamily()
    {
        var store = GetStore(newtonsoftFirst: false);

        var obj = Insert(store, "{ \"Hello\": \"World!\" }", "Object");
        var arr = Insert(store, "[1, 2, 3]", "Array");
        var token = Insert(store, "{ \"Hello\": \"World!\" }", "Token");

        ((string)obj.Object["Hello"]).ShouldBe("World!");
        arr.Array.Count.ShouldBe(3);
        token.Token.ShouldBeOfType<JObject>();

        var data = new MixedView() { Object = JObject.Parse("{\"a\":1}") };

        JToken.Parse((string)BuildParameter(store, data, "Object")).ShouldBe(JObject.Parse("{\"a\":1}"), JToken.EqualityComparer);
    }

    private static MixedView Insert(SchemaStore store, string json, string attributeName)
    {
        var schema = store.GetSchema(typeof(MixedView));
        var buffer = new QueryBuffer(schema, QueryType.List);

        buffer.Insert(json, ("", attributeName));

        return buffer.Commit<MixedView>();
    }

    private static object BuildParameter(SchemaStore store, MixedView data, string attributeName)
    {
        var parameter = new Parameter("P0", store.From(data).Lookup(attributeName));
        var sqlParameter = new SqliteParameter();

        parameter.Build(sqlParameter);

        return sqlParameter.Value;
    }

    private static SchemaStore GetStore(bool newtonsoftFirst)
    {
        var store = DatabaseHelper.Default.GetStore();
        var stj = new JsonBindingConvention(JsonSerializerOptions.Default);
        var newtonsoft = new NewtonsoftJsonBindingConvention(new JsonSerializerSettings());

        if (newtonsoftFirst)
        {
            store.Use(newtonsoft);
            store.Use(stj);
        }
        else
        {
            store.Use(stj);
            store.Use(newtonsoft);
        }

        return store;
    }

    public class MixedView
    {
        [Json]
        public Payload Payload { get; set; }
        public JsonNode Node { get; set; }
        [Json]
        public JsonNode AttributedNode { get; set; }
        public JObject Object { get; set; }
        public JArray Array { get; set; }
        public JToken Token { get; set; }
    }

    public class Payload
    {
        [JsonProperty("renamed")]
        public int Value { get; set; }
    }
}
