using Jerrycurl.Mvc.Sql;
using Jerrycurl.Test.Project.Accessors;
using Jerrycurl.Test.Project.Models;
using Jerrycurl.Vendors.Oracle.Test.Models;
using Shouldly;
using System.Linq;
using System.Text.Json.Nodes;

namespace Jerrycurl.Vendors.Oracle.Test;

// requires Oracle 23ai or later
public class JsonNodeTests
{
    public void Test_JsonNode_Insert_Select()
    {
        Runnable drop = new Runnable();
        Runnable create = new Runnable();

        drop.Sql(@"
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE ""jerry_jsonnodes""';
EXCEPTION
   WHEN OTHERS THEN
      NULL;
END;");

        create.Sql(@"
CREATE TABLE ""jerry_jsonnodes""(
        ""Id"" number(10) NOT NULL,
        ""Document"" json NOT NULL,
        ""NullDocument"" json NULL,
        ""Object"" json NOT NULL,
        ""Array"" json NOT NULL
)");

        Runner.Command(drop);
        Runner.Command(create);

        JsonNodeModel sample = GetSample();

        Runnable<JsonNodeModel, object> insert = new Runnable<JsonNodeModel, object>(sample);

        insert.Sql(@"INSERT INTO ""jerry_jsonnodes"" ( ");
        insert.M(p => p.ColNames());
        insert.Sql(" ) VALUES ( ");
        insert.M(p => p.Pars());
        insert.Sql(")");

        Runner.Command(insert);

        Runnable<object, JsonNodeModel> select = new Runnable<object, JsonNodeModel>();

        select.Sql("SELECT ");
        select.R(p => p.Map());
        select.Sql(@" FROM ""jerry_jsonnodes"" ");
        select.R(p => p.Ali());

        JsonNodeModel fromDb = Runner.Query(select).FirstOrDefault();

        CompareModels(fromDb, sample);

        JsonNodeModel fromDb2 = new JsonNodeModel();
        Runnable<JsonNodeModel, object> bind = new Runnable<JsonNodeModel, object>(fromDb2);

        bind.Sql("SELECT ");
        bind.M(p => p.Cols().As().Props());
        bind.Sql(@" FROM ""jerry_jsonnodes"" ");
        bind.M(p => p.Ali());

        Runner.Command(bind);

        CompareModels(fromDb2, sample);
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
