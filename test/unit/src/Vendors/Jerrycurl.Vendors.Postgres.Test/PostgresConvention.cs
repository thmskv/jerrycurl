using Jerrycurl.Mvc;
using Jerrycurl.Test;
using Npgsql;

namespace Jerrycurl.Vendors.Postgres.Test;

public class PostgresConvention : DatabaseConvention
{
    public override bool Skip => string.IsNullOrEmpty(GetConnectionString());
    public override string SkipReason => "Please configure connection in the 'JERRY_POSTGRES_CONN' environment variable.";

    public override void Configure(DomainOptions options)
    {
        // Npgsql loads database types once per data source, so the extension must exist before the domain connects.
        CreateVectorExtension();

        options.UsePostgres(GetConnectionString());
        options.UseNewtonsoftJson();
    }

    public static string GetConnectionString() => GetEnvironmentVariable("JERRY_POSTGRES_CONN");
    public static NpgsqlConnection GetConnection() => new NpgsqlConnection(GetConnectionString());

    private static void CreateVectorExtension()
    {
        try
        {
            using NpgsqlConnection connection = GetConnection();
            using NpgsqlCommand command = new NpgsqlCommand("CREATE EXTENSION IF NOT EXISTS vector;", connection);

            connection.Open();
            command.ExecuteNonQuery();
        }
        catch (PostgresException)
        {
            // pgvector is not installed on the server; only the vector tests will fail
        }
    }
}
