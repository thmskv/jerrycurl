using Jerrycurl.Mvc.Sql;
using Jerrycurl.Mvc.Sql.SqlServer;
using Jerrycurl.Test.Extensions;
using Jerrycurl.Test.Project.Accessors;
using Jerrycurl.Test.Project.Models;
using Jerrycurl.Vendors.SqlServer.Test.Models;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Jerrycurl.Vendors.SqlServer.Test
{
    public class TvpTests
    {
        public void Test_Tvp_Select()
        {
            Runnable table = new Runnable();

            table.Sql(@"
IF type_id('jerry_tt') IS NOT NULL
    DROP TYPE jerry_tt;

CREATE TYPE jerry_tt AS TABLE
(
    ""Bool"" bit,
    ""Int16"" smallint,
    ""Int32"" int,
    ""Int64"" bigint,
    ""Float"" real,
    ""Double"" float,
    ""Decimal"" decimal(10, 3),
    ""DateTimeOffset"" datetimeoffset(7),
    ""DateTime"" datetime,
    ""DateTime2"" datetime2(7),
    ""Time"" time(7),
    ""String"" nvarchar(MAX),
    ""Bytes"" varbinary(MAX),
    ""Guid"" uniqueidentifier
);");

            Runner.Command(table);

            var inputModel = new TestModel()
            {
                Tvp = new List<TvpModel>()
                {
                    new TvpModel()
                    {
                        Bool = true,
                        Int16 = 1658,
                        Int32 = 4582717,
                        Int64 = 9237938798572,
                        Float = 16.6f,
                        Double = 16.8d,
                        Decimal = 2352.235m,
                        DateTime = new DateTime(1819, 1, 24, 7, 22, 44),
                        DateTime2 = new DateTime(0019, 1, 23, 6, 22, 59),
                        DateTimeOffset = new DateTimeOffset(1819, 1, 22, 13, 11, 22, TimeSpan.FromHours(2)),
                        Time = new TimeSpan(1, 2, 3),
                        String = "Long",
                        Guid = Guid.NewGuid(),
                        Bytes = new byte[] { 1, 2, 3 },
                    },
                    new TvpModel()
                    {
                        Bool = true,
                        Int16 = 1458,
                        Int32 = 45487317,
                        Int64 = 9279238798572,
                        Float = 16.63f,
                        Double = 16.84d,
                        Decimal = 1618616.888m,
                        DateTime = new DateTime(1819, 1, 24, 7, 22, 44),
                        DateTime2 = new DateTime(0019, 1, 23, 6, 22, 59),
                        DateTimeOffset = new DateTimeOffset(1819, 1, 22, 18, 11, 22, TimeSpan.FromHours(2)),
                        Time = new TimeSpan(1, 2, 3),
                        String = "Longer",
                        Guid = Guid.NewGuid(),
                        Bytes = new byte[] { 1, 2, 3, 4, 5, 6 },
                    },
                }
            };

            Runnable<TestModel, TestModel> select = new Runnable<TestModel, TestModel>(inputModel);

            select.Sql("SELECT ");
            select.R(p => p.Star(m => m.Tvp, "X"));
            select.Sql(" FROM ");
            select.M(p => p.Tvp(m => m.Tvp, "X"));

            TestModel result = Runner.Aggregate(select);

            inputModel.Tvp.ShouldBeSameAsJson(result.Tvp);
        }
    }
}
