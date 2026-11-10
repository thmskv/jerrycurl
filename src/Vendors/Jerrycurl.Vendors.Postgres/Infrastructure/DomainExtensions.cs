using System;
using Jerrycurl.Vendors.Postgres;
using Jerrycurl.Vendors.Postgres.Metadata;
using Npgsql;
using Pgvector.Npgsql;

namespace Jerrycurl.Mvc;

public static class DomainExtensions
{
    /// <summary>
    /// Configures the current domain to connect to a PostgreSQL database with a specified connection string.
    /// Support for the pgvector types (<c>vector</c>, <c>halfvec</c> and <c>sparsevec</c>) is enabled automatically.
    /// </summary>
    /// <param name="options">A <see cref="DomainOptions"/> instance from the <see cref="IDomain.Configure(DomainOptions)"/> method.</param>
    /// <param name="connectionString">Connection string specifying the details of the connection.</param>
    public static DomainOptions UsePostgres(this DomainOptions options, string connectionString)
    {
        NpgsqlDataSourceBuilder builder = new NpgsqlDataSourceBuilder(connectionString);

        builder.UseVector();

        return options.UsePostgres(builder.Build());
    }

    /// <summary>
    /// Configures the current domain to connect to a PostgreSQL database through a specified data source.
    /// Call <c>UseVector()</c> on the <see cref="NpgsqlDataSourceBuilder"/> to enable support for the pgvector types.
    /// </summary>
    /// <param name="options">A <see cref="DomainOptions"/> instance from the <see cref="IDomain.Configure(DomainOptions)"/> method.</param>
    /// <param name="dataSource">Data source used to create connections.</param>
    public static DomainOptions UsePostgres(this DomainOptions options, NpgsqlDataSource dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        options.ConnectionFactory = () => dataSource.CreateConnection();
        options.Dialect = new PostgresDialect();
        options.Use(new PostgresContractResolver());

        return options;
    }
}
