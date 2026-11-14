using Jerrycurl.Mvc.Sql;
using Jerrycurl.Test.Project.Accessors;
using Jerrycurl.Test.Project.Models;
using Jerrycurl.Vendors.MySql.Test.Models;
using Shouldly;
using System.Linq;

namespace Jerrycurl.Vendors.MySql.Test;

// requires MySQL 9.0 or later
public class VectorTests
{
    public void Test_Vector_Insert_Select()
    {
        Runnable table = new Runnable();

        table.Sql(@"
DROP TABLE IF EXISTS jerry_vectors;
CREATE TABLE jerry_vectors(
        `Id` int NOT NULL,
        `Embedding` vector(3) NOT NULL,
        `NullEmbedding` vector(3) NULL
);");

        Runner.Command(table);

        VectorModel sample = GetSample();

        Runnable<VectorModel, object> insert = new Runnable<VectorModel, object>(sample);

        insert.Sql("INSERT INTO jerry_vectors ( ");
        insert.M(p => p.ColNames());
        insert.Sql(" ) VALUES ( ");
        insert.M(p => p.Pars());
        insert.Sql(");");

        Runner.Command(insert);

        Runnable<object, VectorModel> select = new Runnable<object, VectorModel>();

        select.Sql("SELECT ");
        select.R(p => p.Map());
        select.Sql(" FROM jerry_vectors ");
        select.R(p => p.Ali());
        select.Sql(";");

        VectorModel fromDb = Runner.Query(select).FirstOrDefault();

        CompareModels(fromDb, sample);

        VectorModel fromDb2 = new VectorModel();
        Runnable<VectorModel, object> bind = new Runnable<VectorModel, object>(fromDb2);

        bind.Sql("SELECT ");
        bind.M(p => p.Cols().As().Props());
        bind.Sql(" FROM jerry_vectors ");
        bind.M(p => p.Ali());
        bind.Sql(";");

        Runner.Command(bind);

        CompareModels(fromDb2, sample);
    }

    public void Test_Vector_IsReadableByServerFunctions()
    {
        Runnable table = new Runnable();

        table.Sql(@"
DROP TABLE IF EXISTS jerry_vectors;
CREATE TABLE jerry_vectors(
        `Id` int NOT NULL,
        `Embedding` vector(3) NOT NULL,
        `NullEmbedding` vector(3) NULL
);");

        Runner.Command(table);

        Runnable<VectorModel, object> insert = new Runnable<VectorModel, object>(GetSample());

        insert.Sql("INSERT INTO jerry_vectors ( ");
        insert.M(p => p.ColNames());
        insert.Sql(" ) VALUES ( ");
        insert.M(p => p.Pars());
        insert.Sql(");");

        Runner.Command(insert);

        Runnable<object, string> select = new Runnable<object, string>();

        select.Sql("SELECT VECTOR_TO_STRING(`Embedding`) AS ");
        select.R(p => p.Prop());
        select.Sql(" FROM jerry_vectors;");

        string text = Runner.Query(select).FirstOrDefault();

        text.ShouldNotBeNull();
        text.Replace(" ", "").ShouldBe("[1.50000e+00,-2.00000e+00,3.00000e+00]");
    }

    private static VectorModel GetSample() => new VectorModel()
    {
        Id = 1,
        Embedding = [1.5f, -2, 3],
        NullEmbedding = null,
    };

    private static void CompareModels(VectorModel fromDb, VectorModel sample)
    {
        fromDb.ShouldNotBeNull();
        fromDb.Id.ShouldBe(sample.Id);
        fromDb.Embedding.ShouldBe(sample.Embedding);
        fromDb.NullEmbedding.ShouldBeNull();
    }
}
