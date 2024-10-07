using Jerrycurl.Tools.Orm;
using Jerrycurl.Tools.Vendors.MySql;
using Jerrycurl.Tools.Vendors.Oracle;
using Jerrycurl.Tools.Vendors.Postgres;
using Jerrycurl.Tools.Vendors.Sqlite;
using Jerrycurl.Tools.Vendors.SqlServer;

namespace Jerrycurl.Tools.Vendors;

public static class ToolResolver
{
    public static OrmTool GetOrmTool(string vendor) => vendor switch
    {
        "sqlserver" => new SqlServerOrmTool(),
        "sqlite" => new SqliteOrmTool(),
        "postgres" => new PostgresOrmTool(),
        "mysql" => new MySqlOrmTool(),
        "oracle" => new OracleOrmTool(),
        _ => null,
    };
}
