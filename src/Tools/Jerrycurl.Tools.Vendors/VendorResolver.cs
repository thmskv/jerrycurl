using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jerrycurl.Tools.Orm;
using Jerrycurl.Tools.Vendors.SqlServer;

namespace Jerrycurl.Tools.Vendors
{
    public static class VendorResolver
    {
        public static OrmCommand GetOrmCommand(string vendor) => vendor switch
        {
            "sqlserver" => new SqlServerOrmCommand(),
            _ => throw new NotSupportedException($"Unknown vendor '{vendor}'."),
        };
    }
}
