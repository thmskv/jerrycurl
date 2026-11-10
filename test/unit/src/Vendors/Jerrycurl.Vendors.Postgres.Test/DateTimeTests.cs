using Jerrycurl.Mvc.Sql;
using Jerrycurl.Test.Project.Accessors;
using Jerrycurl.Test.Project.Models;
using Jerrycurl.Vendors.Postgres.Test.Models;
using Shouldly;
using System;
using System.Linq;

namespace Jerrycurl.Vendors.Postgres.Test;

public class DateTimeTests
{
    public void Test_DateOnly_TimeOnly_Insert_Select()
    {
        Runnable table = new Runnable();

        table.Sql(@"
DROP TABLE IF EXISTS jerry_dateonly;
CREATE TABLE jerry_dateonly(
        ""Date"" date NOT NULL,
        ""NullDate"" date NULL,
        ""NotNullDate"" date NULL,
        ""Time"" time NOT NULL,
        ""NullTime"" time NULL,
        ""NotNullTime"" time NULL
);");

        Runner.Command(table);

        DateTimeModel sample = new DateTimeModel()
        {
            Date = new DateOnly(1819, 5, 4),
            NullDate = null,
            NotNullDate = new DateOnly(2024, 2, 29),
            Time = new TimeOnly(23, 59, 59, 999),
            NullTime = null,
            NotNullTime = new TimeOnly(0, 0, 1),
        };

        Runnable<DateTimeModel, object> insert = new Runnable<DateTimeModel, object>(sample);

        insert.Sql("INSERT INTO jerry_dateonly ( ");
        insert.M(p => p.ColNames());
        insert.Sql(" ) VALUES ( ");
        insert.M(p => p.Pars());
        insert.Sql(");");

        Runner.Command(insert);

        Runnable<object, DateTimeModel> select = new Runnable<object, DateTimeModel>();

        select.Sql("SELECT ");
        select.R(p => p.Map());
        select.Sql(" FROM jerry_dateonly ");
        select.R(p => p.Ali());
        select.Sql(";");

        DateTimeModel fromDb = Runner.Query(select).FirstOrDefault();

        Compare(fromDb, sample);

        DateTimeModel fromDb2 = new DateTimeModel();
        Runnable<DateTimeModel, object> bind = new Runnable<DateTimeModel, object>(fromDb2);

        bind.Sql("SELECT ");
        bind.M(p => p.Cols().As().Props());
        bind.Sql(" FROM jerry_dateonly ");
        bind.M(p => p.Ali());
        bind.Sql(";");

        Runner.Command(bind);

        Compare(fromDb2, sample);
    }

    private static void Compare(DateTimeModel fromDb, DateTimeModel sample)
    {
        fromDb.ShouldNotBeNull();
        fromDb.Date.ShouldBe(sample.Date);
        fromDb.NullDate.ShouldBeNull();
        fromDb.NotNullDate.ShouldBe(sample.NotNullDate);
        fromDb.Time.ShouldBe(sample.Time);
        fromDb.NullTime.ShouldBeNull();
        fromDb.NotNullTime.ShouldBe(sample.NotNullTime);
    }
}
