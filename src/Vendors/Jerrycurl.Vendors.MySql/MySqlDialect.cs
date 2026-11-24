using Jerrycurl.Razor;

namespace Jerrycurl.Vendors.MySql;

public class MySqlDialect : IsoDialect
{
    protected override char IdentifierQuote => '`';
}
