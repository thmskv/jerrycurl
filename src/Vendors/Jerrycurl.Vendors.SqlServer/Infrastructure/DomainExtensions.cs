using Jerrycurl.Vendors.SqlServer.Metadata;
using Microsoft.Data.SqlClient;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Jerrycurl.Mvc;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class DomainExtensions
{
    /// <summary>
    /// Configures the current domain to connect to a Microsoft SQL Server database with a specified connection string.
    /// </summary>
    /// <param name="options">A <see cref="DomainOptions"/> instance from the <see cref="IDomain.Configure(DomainOptions)"/> method.</param>
    /// <param name="connectionString">Connection string specifying the details of the connection.</param>
    public static DomainOptions UseSqlServer(this DomainOptions options, string connectionString)
    {
        options.ConnectionFactory = () => new SqlConnection(connectionString);
        options.Use(new SqlServerContractResolver());
        options.Sql ??= new SqlOptions();
        options.Sql.MaxParameters = 2098;

        return options;
    }
}
