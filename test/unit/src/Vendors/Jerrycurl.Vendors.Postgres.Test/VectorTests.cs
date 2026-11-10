using Jerrycurl.Mvc.Sql;
using Jerrycurl.Test.Project.Accessors;
using Jerrycurl.Test.Project.Models;
using Jerrycurl.Vendors.Postgres.Test.Models;
using Pgvector;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Vendors.Postgres.Test;

public class VectorTests
{
    public void Test_Vector_Insert_Select()
    {
        Runnable table = new Runnable();

        table.Sql(@"
DROP TABLE IF EXISTS jerry_vectors;
CREATE TABLE jerry_vectors(
        ""Id"" int NOT NULL,
        ""Embedding"" vector(3) NOT NULL,
        ""NullEmbedding"" vector(3) NULL,
        ""HalfEmbedding"" halfvec(3) NOT NULL,
        ""SparseEmbedding"" sparsevec(5) NOT NULL
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
        ""Id"" int NOT NULL,
        ""Embedding"" vector(3) NOT NULL
);
INSERT INTO jerry_vectors_knn VALUES (1, '[1,1,1]'), (2, '[2,2,2]'), (3, '[1,1,2]');");

        Runner.Command(table);

        VectorModel target = new VectorModel() { Embedding = new Vector(new float[] { 1, 1, 1 }) };
        Runnable<VectorModel, int> select = new Runnable<VectorModel, int>(target);

        select.Sql(@"SELECT ""Id"" AS ");
        select.R(p => p.Prop());
        select.Sql(@" FROM jerry_vectors_knn ORDER BY ""Embedding"" <-> ");
        select.M(p => p.Par(m => m.Embedding));
        select.Sql(";");

        IList<int> ids = Runner.Query(select);

        ids.ShouldBe([1, 3, 2]);
    }

    private static VectorModel GetSample() => new VectorModel()
    {
        Id = 1,
        Embedding = new Vector(new float[] { 1.5f, -2, 3 }),
        NullEmbedding = null,
        HalfEmbedding = new HalfVector(new Half[] { (Half)1, (Half)2.5, (Half)(-3) }),
        SparseEmbedding = new SparseVector(new float[] { 1, 0, 2, 0, 3 }),
    };

    private static void CompareModels(VectorModel fromDb, VectorModel sample)
    {
        fromDb.ShouldNotBeNull();
        fromDb.Id.ShouldBe(sample.Id);
        fromDb.Embedding.ToArray().ShouldBe(sample.Embedding.ToArray());
        fromDb.NullEmbedding.ShouldBeNull();
        fromDb.HalfEmbedding.ToArray().ShouldBe(sample.HalfEmbedding.ToArray());
        fromDb.SparseEmbedding.ToArray().ShouldBe(sample.SparseEmbedding.ToArray());
    }
}
