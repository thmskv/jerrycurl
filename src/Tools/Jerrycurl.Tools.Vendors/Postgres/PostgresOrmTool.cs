using Npgsql;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Tools.Orm.Model;
using Jerrycurl.Tools.Orm;

namespace Jerrycurl.Tools.Vendors.Postgres
{
    public class PostgresOrmTool : OrmTool
    {
        protected override DbConnection GetConnection(OrmToolOptions options) => new NpgsqlConnection(options.Connection);

        protected override async Task<SchemaModel> BuildSchemaAsync(SchemaBuilder builder, CancellationToken cancellationToken = default)
        {
            await using DbConnection connection = await this.OpenConnectionAsync(builder.Options);

            this.AddTypeMappings(builder);

            builder.SetFlag("defaultSchema", "public", overwrite: false);

            using (DbCommand tablesAndColumns = connection.CreateCommand())
            {
                tablesAndColumns.CommandText = @"SELECT *
                                                 FROM INFORMATION_SCHEMA.TABLES T1
                                                 INNER JOIN INFORMATION_SCHEMA.COLUMNS T2 ON T2.TABLE_SCHEMA = T1.TABLE_SCHEMA AND T2.TABLE_NAME = T1.TABLE_NAME
                                                 WHERE T1.TABLE_TYPE = 'BASE TABLE' AND T1.TABLE_SCHEMA NOT IN ('information_schema','pg_catalog')
                                                 ORDER BY T1.TABLE_SCHEMA, T1.TABLE_NAME, T2.ORDINAL_POSITION";

                await this.AddTablesAndColumnAsync(builder, tablesAndColumns);
            }

            using (DbCommand primaryKeys = connection.CreateCommand())
            {
                primaryKeys.CommandText = @"SELECT T2.* FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS T1
                                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE T2 ON T2.CONSTRAINT_CATALOG = T1.CONSTRAINT_CATALOG AND T2.CONSTRAINT_SCHEMA = T1.CONSTRAINT_SCHEMA AND T2.CONSTRAINT_NAME = T1.CONSTRAINT_NAME
                                            WHERE T1.CONSTRAINT_TYPE = 'PRIMARY KEY' AND T1.TABLE_SCHEMA NOT IN ('information_schema','pg_catalog')
                                            ORDER BY T2.ORDINAL_POSITION";

                await this.AddPrimaryKeysAsync(builder, primaryKeys);
            }

            using (DbCommand foreignKeys = connection.CreateCommand())
            {
                foreignKeys.CommandText = @"SELECT T2.*, T1.UNIQUE_CONSTRAINT_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS T1
                                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS T2  ON T2.CONSTRAINT_CATALOG = T1.CONSTRAINT_CATALOG  AND T2.CONSTRAINT_SCHEMA = T1.CONSTRAINT_SCHEMA AND T2.CONSTRAINT_NAME = T1.CONSTRAINT_NAME
                                            WHERE T2.TABLE_SCHEMA NOT IN ('information_schema','pg_catalog')
                                            ORDER BY T2.ORDINAL_POSITION";

                await this.AddForeignKeysAsync(builder, foreignKeys);
            }

            return builder.Model;
        }

        private async Task AddTablesAndColumnAsync(SchemaBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableSchema = tuple["table_schema"] as string;
                string tableName = tuple["table_name"] as string;
                string columnName = tuple["column_name"] as string;
                string typeName = tuple["data_type"] as string;
                bool isNullable = (tuple["is_nullable"] as string == "YES");
                bool isIdentity = (tuple["is_identity"] as string == "YES" || tuple["serial_seq"] != null);
                bool ignoreTable = false;

                builder.AddColumn(tableSchema, tableName, columnName, typeName, isNullable: isNullable, isIdentity: isIdentity, ignoreTable: ignoreTable);
            }
        }

        private async Task AddPrimaryKeysAsync(SchemaBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableSchema = tuple["table_schema"] as string;
                string tableName = tuple["table_name"] as string;
                string columnName = tuple["column_name"] as string;
                string keyName = tuple["constraint_name"] as string;
                int keyIndex = int.Parse(tuple["ordinal_position"]?.ToString());

                builder.AddKey(tableSchema, tableName, columnName, keyName, keyIndex);
            }
        }

        public async Task AddForeignKeysAsync(SchemaBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableSchema = tuple["table_schema"] as string;
                string tableName = tuple["table_name"] as string;
                string columnName = tuple["column_name"] as string;
                string uniqueName = tuple["unique_constraint_name"] as string;
                string foreignName = tuple["constraint_name"] as string;
                int keyIndex = int.Parse(tuple["ordinal_position"]?.ToString());

                builder.AddReference(tableSchema, tableName, columnName, foreignName, uniqueName, keyIndex);
            }
        }

        private void AddTypeMappings(SchemaBuilder builder)
        {
            builder.AddType("boolean", "bool", true);
            builder.AddType("smallint", "short", true);
            builder.AddType("integer", "int", true);
            builder.AddType("bigint", "long", true);
            builder.AddType("real", "float", true);
            builder.AddType("double precision", "double", true);
            builder.AddType("numeric", "decimal", true);
            builder.AddType("money", "decimal", true);
            builder.AddType("text", "string", false);
            builder.AddType("character varying", "string", false);
            builder.AddType("character", "string", false);
            builder.AddType("citext", "string", false);
            builder.AddType("json", "string", false);
            builder.AddType("jsonb", "string", false);
            builder.AddType("xml", "string", false);
            builder.AddType("name", "string", false);
            builder.AddType("bit", "System.Collections.BitArray", false);
            builder.AddType("hstore", "System.Collections.IDictionary<string, string>", false);
            builder.AddType("uuid", "Guid", true);
            builder.AddType("cidr", "(System.Net.IPAddress, int)", true);
            builder.AddType("inet", "System.Net.IPAddress", false);
            builder.AddType("macaddr", "System.Net.NetworkInformation.PhysicalAddress", true);
            builder.AddType("date", "DateTime", true);
            builder.AddType("timestamp", "DateTime", true);
            builder.AddType("timestamp with time zone", "DateTimeOffset", true);
            builder.AddType("timestamp without time zone", "DateTime", true);
            builder.AddType("time", "TimeSpan", true);
            builder.AddType("time with time zone", "DateTimeOffset", true);
            builder.AddType("time without time zone", "TimeSpan", true);
            builder.AddType("interval", "TimeSpan", true);
            builder.AddType("bytea", "byte[]", false);
            builder.AddType("oid", "uint", true);
            builder.AddType("xid", "uint", true);
            builder.AddType("cid", "uint", true);
            builder.AddType("oidvector", "uint[]", false);
            builder.AddType("ARRAY", "Array", false);
        }
    }
}
