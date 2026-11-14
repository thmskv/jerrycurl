using Jerrycurl.Mvc.Sql;
using Jerrycurl.Test.Project.Accessors;
using Jerrycurl.Test.Project.Models;
using Jerrycurl.Vendors.Oracle.Test.Models;
using Shouldly;
using System.Linq;

namespace Jerrycurl.Vendors.Oracle.Test;

// requires Oracle 23ai or later
public class BooleanTests
{
    public void Test_Boolean_Insert_Select()
    {
        Runnable drop = new Runnable();
        Runnable create = new Runnable();

        drop.Sql(@"
BEGIN
   EXECUTE IMMEDIATE 'DROP TABLE ""jerry_booleans""';
EXCEPTION
   WHEN OTHERS THEN
      NULL;
END;");

        create.Sql(@"
CREATE TABLE ""jerry_booleans""(
        ""True"" boolean NOT NULL,
        ""False"" boolean NOT NULL,
        ""Null"" boolean NULL,
        ""NotNull"" boolean NULL
)");

        Runner.Command(drop);
        Runner.Command(create);

        BooleanModel sample = new BooleanModel() { True = true, False = false, Null = null, NotNull = true };

        Runnable<BooleanModel, object> insert = new Runnable<BooleanModel, object>(sample);

        insert.Sql(@"INSERT INTO ""jerry_booleans"" ( ");
        insert.M(p => p.ColNames());
        insert.Sql(" ) VALUES ( ");
        insert.M(p => p.Pars());
        insert.Sql(")");

        Runner.Command(insert);

        Runnable<object, BooleanModel> select = new Runnable<object, BooleanModel>();

        select.Sql("SELECT ");
        select.R(p => p.Map());
        select.Sql(@" FROM ""jerry_booleans"" ");
        select.R(p => p.Ali());

        BooleanModel fromDb = Runner.Query(select).FirstOrDefault();

        fromDb.ShouldNotBeNull();
        fromDb.True.ShouldBeTrue();
        fromDb.False.ShouldBeFalse();
        fromDb.Null.ShouldBeNull();
        fromDb.NotNull.ShouldBe(true);
    }
}
