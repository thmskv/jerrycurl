using Jerrycurl.Tools.Info;
#if NET20_BASE
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif

namespace Jerrycurl.Tools.Vendors.SqlServer
{
    public class SqlServerInfoCommand : InfoCommand
    {
        public override string Vendor => "Microsoft SQL Server";
        public override string Connector => typeof(SqlConnection).Assembly.GetName().Name;
        public override string ConnectorVersion => typeof(SqlConnection).Assembly.GetName().Version.ToString();
    }
}
