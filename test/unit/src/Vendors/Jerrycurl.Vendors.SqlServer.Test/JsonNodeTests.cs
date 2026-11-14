using Jerrycurl.Mvc.Sql;
using Jerrycurl.Test.Project.Accessors;
using Jerrycurl.Test.Project.Models;
using Jerrycurl.Vendors.SqlServer.Test.Models;
using Shouldly;
using System.Linq;
using System.Text.Json.Nodes;

namespace Jerrycurl.Vendors.SqlServer.Test;

// requires SQL Server 2025 or later
public class JsonNodeTests
{
    public void Test_JsonType_Insert_Select()
    {
        Runnable table = new Runnable();

        table.Sql(@"
DROP TABLE IF EXISTS jerry_jsonnodes;
CREATE TABLE jerry_jsonnodes(
        [Id] int NOT NULL,
        [Document] json NOT NULL,
        [NullDocument] json NULL,
        [Object] json NOT NULL,
        [Array] json NOT NULL
);");

        Runner.Command(table);

        JsonNodeModel sample = GetSample();

        Runnable<JsonNodeModel, object> insert = new Runnable<JsonNodeModel, object>(sample);

        insert.Sql("INSERT INTO jerry_jsonnodes ( ");
        insert.M(p => p.ColNames());
        insert.Sql(" ) VALUES ( ");
        insert.M(p => p.Pars());
        insert.Sql(");");

        Runner.Command(insert);

        Runnable<object, JsonNodeModel> select = new Runnable<object, JsonNodeModel>();

        select.Sql("SELECT ");
        select.R(p => p.Map());
        select.Sql(" FROM jerry_jsonnodes ");
        select.R(p => p.Ali());
        select.Sql(";");

        JsonNodeModel fromDb = Runner.Query(select).FirstOrDefault();

        CompareModels(fromDb, sample);

        JsonNodeModel fromDb2 = new JsonNodeModel();
        Runnable<JsonNodeModel, object> bind = new Runnable<JsonNodeModel, object>(fromDb2);

        bind.Sql("SELECT ");
        bind.M(p => p.Cols().As().Props());
        bind.Sql(" FROM jerry_jsonnodes ");
        bind.M(p => p.Ali());
        bind.Sql(";");

        Runner.Command(bind);

        CompareModels(fromDb2, sample);
    }

    public void Test_JsonType_Query_WithJsonValue()
    {
        Runnable table = new Runnable();

        table.Sql(@"
DROP TABLE IF EXISTS jerry_jsonnodes_query;
CREATE TABLE jerry_jsonnodes_query(
        [Id] int NOT NULL,
        [Document] json NOT NULL
);
INSERT INTO jerry_jsonnodes_query VALUES (1, '{""a"":1}'), (2, '{""a"":2}');");

        Runner.Command(table);

        Runnable<object, int> select = new Runnable<object, int>();

        select.Sql("SELECT [Id] AS ");
        select.R(p => p.Prop());
        select.Sql(" FROM jerry_jsonnodes_query WHERE JSON_VALUE([Document], '$.a') = 2;");

        Runner.Query(select).ShouldBe([2]);
    }

    private static JsonNodeModel GetSample() => new JsonNodeModel()
    {
        Id = 1,
        Document = JsonNode.Parse(@"{""name"":""jerry"",""tags"":[""a"",""b""],""nested"":{""value"":12.5}}"),
        NullDocument = null,
        Object = new JsonObject() { ["key"] = "value" },
        Array = new JsonArray(1, 2, 3),
    };

    private static void CompareModels(JsonNodeModel fromDb, JsonNodeModel sample)
    {
        fromDb.ShouldNotBeNull();
        fromDb.Id.ShouldBe(sample.Id);
        JsonNode.DeepEquals(fromDb.Document, sample.Document).ShouldBeTrue();
        fromDb.NullDocument.ShouldBeNull();
        JsonNode.DeepEquals(fromDb.Object, sample.Object).ShouldBeTrue();
        JsonNode.DeepEquals(fromDb.Array, sample.Array).ShouldBeTrue();
    }
}
