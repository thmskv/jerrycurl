using Jerrycurl.Mvc.Sql;
using Jerrycurl.Test.Project.Accessors;
using Jerrycurl.Test.Project.Models;
using Jerrycurl.Vendors.Postgres.Test.Models;
using NpgsqlTypes;
using Shouldly;
using System.Linq;

namespace Jerrycurl.Vendors.Postgres.Test;

public class TextSearchTests
{
    public void Test_TsVector_TsQuery_Insert_Select()
    {
        Runnable table = new Runnable();

        table.Sql(@"
DROP TABLE IF EXISTS jerry_textsearch;
CREATE TABLE jerry_textsearch(
        ""Document"" tsvector NOT NULL,
        ""NullDocument"" tsvector NULL,
        ""Query"" tsquery NOT NULL
);");

        Runner.Command(table);

        TextSearchModel sample = new TextSearchModel()
        {
            Document = NpgsqlTsVector.Parse("jerry:1 curl:2"),
            NullDocument = null,
            Query = NpgsqlTsQuery.Parse("jerry & curl"),
        };

        Runnable<TextSearchModel, object> insert = new Runnable<TextSearchModel, object>(sample);

        insert.Sql("INSERT INTO jerry_textsearch ( ");
        insert.M(p => p.ColNames());
        insert.Sql(" ) VALUES ( ");
        insert.M(p => p.Pars());
        insert.Sql(");");

        Runner.Command(insert);

        Runnable<object, TextSearchModel> select = new Runnable<object, TextSearchModel>();

        select.Sql("SELECT ");
        select.R(p => p.Map());
        select.Sql(" FROM jerry_textsearch ");
        select.R(p => p.Ali());
        select.Sql(@" WHERE ""Document"" @@ ""Query"";");

        TextSearchModel fromDb = Runner.Query(select).FirstOrDefault();

        fromDb.ShouldNotBeNull();
        fromDb.Document.ToString().ShouldBe(sample.Document.ToString());
        fromDb.NullDocument.ShouldBeNull();
        fromDb.Query.ToString().ShouldBe(sample.Query.ToString());
    }
}
