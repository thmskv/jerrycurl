using Jerrycurl.Mvc.Sql;
using Jerrycurl.Test.Project.Accessors;
using Jerrycurl.Test.Project.Models;
using Jerrycurl.Vendors.Oracle.Test.Models;
using Oracle.ManagedDataAccess.Types;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Vendors.Oracle.Test;

// requires Oracle 23ai or later
public class VectorTests
{
    public void Test_Vector_Insert_Select()
    {
        Runnable drop = new Runnable();
        Runnable create = new Runnable();

        drop.Sql(@"
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE ""jerry_vectors""';
EXCEPTION
   WHEN OTHERS THEN
      NULL;
END;");

        create.Sql(@"
CREATE TABLE ""jerry_vectors""(
        ""Id"" number(10) NOT NULL,
        ""Embedding"" vector(3, float32) NOT NULL,
        ""NullEmbedding"" vector(3, float32) NULL
)");

        Runner.Command(drop);
        Runner.Command(create);

        VectorModel sample = GetSample();

        Runnable<VectorModel, object> insert = new Runnable<VectorModel, object>(sample);

        insert.Sql(@"INSERT INTO ""jerry_vectors"" ( ");
        insert.M(p => p.ColNames());
        insert.Sql(" ) VALUES ( ");
        insert.M(p => p.Pars());
        insert.Sql(")");

        Runner.Command(insert);

        Runnable<object, VectorModel> select = new Runnable<object, VectorModel>();

        select.Sql("SELECT ");
        select.R(p => p.Map());
        select.Sql(@" FROM ""jerry_vectors"" ");
        select.R(p => p.Ali());

        VectorModel fromDb = Runner.Query(select).FirstOrDefault();

        CompareModels(fromDb, sample);

        VectorModel fromDb2 = new VectorModel();
        Runnable<VectorModel, object> bind = new Runnable<VectorModel, object>(fromDb2);

        bind.Sql("SELECT ");
        bind.M(p => p.Cols().As().Props());
        bind.Sql(@" FROM ""jerry_vectors"" ");
        bind.M(p => p.Ali());

        Runner.Command(bind);

        CompareModels(fromDb2, sample);
    }

    public void Test_Vector_Parameter_InDistanceQuery()
    {
        Runnable drop = new Runnable();
        Runnable create = new Runnable();

        drop.Sql(@"
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE ""jerry_vectors_knn""';
EXCEPTION
   WHEN OTHERS THEN
      NULL;
END;");

        create.Sql(@"
CREATE TABLE ""jerry_vectors_knn""(
        ""Id"" number(10) NOT NULL,
        ""Embedding"" vector(3, float32) NOT NULL
)");

        Runnable insert = new Runnable();

        insert.Sql(@"
INSERT INTO ""jerry_vectors_knn"" VALUES (1, '[1,1,1]'), (2, '[2,2,2]'), (3, '[1,1,2]')");

        Runner.Command(drop);
        Runner.Command(create);
        Runner.Command(insert);

        VectorModel target = new VectorModel() { Embedding = new OracleVector(new float[] { 1, 1, 1 }) };
        Runnable<VectorModel, int> select = new Runnable<VectorModel, int>(target);

        select.Sql(@"SELECT ""Id"" AS ");
        select.R(p => p.Prop());
        select.Sql(@" FROM ""jerry_vectors_knn"" ORDER BY VECTOR_DISTANCE(""Embedding"", ");
        select.M(p => p.Par(m => m.Embedding));
        select.Sql(", EUCLIDEAN)");

        IList<int> ids = Runner.Query(select);

        ids.ShouldBe([1, 3, 2]);
    }

    private static VectorModel GetSample() => new VectorModel()
    {
        Id = 1,
        Embedding = new OracleVector(new float[] { 1.5f, -2, 3 }),
        NullEmbedding = null,
    };

    private static void CompareModels(VectorModel fromDb, VectorModel sample)
    {
        fromDb.ShouldNotBeNull();
        fromDb.Id.ShouldBe(sample.Id);
        fromDb.Embedding.ToFloatArray().ShouldBe(sample.Embedding.ToFloatArray());
        fromDb.NullEmbedding.ShouldBeNull();
    }
}
