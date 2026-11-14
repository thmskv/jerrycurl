using Jerrycurl.Mvc.Sql;
using Jerrycurl.Test.Project.Accessors;
using Jerrycurl.Test.Project.Models;
using Jerrycurl.Vendors.SqlServer.Test.Models;
using Microsoft.Data.SqlTypes;
using Shouldly;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Vendors.SqlServer.Test;

// requires SQL Server 2025 or later
public class VectorTests
{
    public void Test_Vector_Insert_Select()
    {
        Runnable table = new Runnable();

        table.Sql(@"
DROP TABLE IF EXISTS jerry_vectors;
CREATE TABLE jerry_vectors(
        [Id] int NOT NULL,
        [Embedding] vector(3) NOT NULL,
        [NullableEmbedding] vector(3) NULL,
        [NullEmbedding] vector(3) NULL
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

    public void Test_Vector_Parameter_InDistanceQuery()
    {
        Runnable table = new Runnable();

        table.Sql(@"
DROP TABLE IF EXISTS jerry_vectors_knn;
CREATE TABLE jerry_vectors_knn(
        [Id] int NOT NULL,
        [Embedding] vector(3) NOT NULL
);
INSERT INTO jerry_vectors_knn VALUES (1, '[1,1,1]'), (2, '[2,2,2]'), (3, '[1,1,2]');");

        Runner.Command(table);

        VectorModel target = new VectorModel() { Embedding = new SqlVector<float>(new float[] { 1, 1, 1 }) };
        Runnable<VectorModel, int> select = new Runnable<VectorModel, int>(target);

        select.Sql("SELECT [Id] AS ");
        select.R(p => p.Prop());
        select.Sql(" FROM jerry_vectors_knn ORDER BY VECTOR_DISTANCE('euclidean', [Embedding], ");
        select.M(p => p.Par(m => m.Embedding));
        select.Sql(");");

        IList<int> ids = Runner.Query(select);

        ids.ShouldBe([1, 3, 2]);
    }

    private static VectorModel GetSample() => new VectorModel()
    {
        Id = 1,
        Embedding = new SqlVector<float>(new float[] { 1.5f, -2, 3 }),
        NullableEmbedding = new SqlVector<float>(new float[] { 0.25f, 0, -1 }),
        NullEmbedding = null,
    };

    private static void CompareModels(VectorModel fromDb, VectorModel sample)
    {
        fromDb.ShouldNotBeNull();
        fromDb.Id.ShouldBe(sample.Id);
        fromDb.Embedding.Memory.ToArray().ShouldBe(sample.Embedding.Memory.ToArray());
        fromDb.NullableEmbedding.ShouldNotBeNull();
        fromDb.NullableEmbedding.Value.Memory.ToArray().ShouldBe(sample.NullableEmbedding.Value.Memory.ToArray());
        fromDb.NullEmbedding.ShouldBeNull();
    }
}
