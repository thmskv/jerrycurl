using Jerrycurl.Vendors.Sqlite.Metadata;
#if NET20_BASE
using SqliteConnection = System.Data.SQLite.SQLiteConnection;
#else
using Microsoft.Data.Sqlite;
#endif

namespace Jerrycurl.Mvc
{
    public static class DomainExtensions
    {
        /// <summary>
        /// Configures the current domain to connect to a SQLite database with a specified connection string.
        /// </summary>
        /// <param name="options">A <see cref="DomainOptions"/> instance from the <see cref="IDomain.Configure(DomainOptions)"/> method.</param>
        /// <param name="connectionString">Connection string specifying the details of the connection.</param>
        public static DomainOptions UseSqlite(this DomainOptions options, string connectionString)
        {
            options.ConnectionFactory = () => new SqliteConnection(connectionString);
            options.Use(new SqliteContractResolver());

            return options;
        }
    }
}
