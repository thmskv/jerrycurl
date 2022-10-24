using Jerrycurl.Tools.Info;
#if NET20_BASE
using SqliteConnection = System.Data.SQLite.SQLiteConnection;
#else
using Microsoft.Data.Sqlite;
#endif

namespace Jerrycurl.Tools.Vendors.Sqlite
{
    public class SqliteInfoCommand : InfoCommand
    {
        public override string Vendor => "SQLite";
        public override string Connector => typeof(SqliteConnection).Assembly.GetName().Name;
        public override string ConnectorVersion => typeof(SqliteConnection).Assembly.GetName().Version.ToString();
    }
}
