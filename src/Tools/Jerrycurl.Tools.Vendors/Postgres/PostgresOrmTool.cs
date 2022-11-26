using Npgsql;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Tools.Orm.Model;
using static Jerrycurl.Tools.Orm.Model.DatabaseModel;
using Jerrycurl.Tools.Orm;

namespace Jerrycurl.Tools.Vendors.Postgres
{
    public class PostgresOrmTool : OrmTool
    {
        protected override DbConnection GetConnection(OrmToolOptions options) => new NpgsqlConnection(options.Connection);

        protected override async Task<DatabaseModel> GetDatabaseModelAsync(OrmToolOptions options, CancellationToken cancellationToken = default)
        {
            await using DbConnection connection = await this.OpenConnectionAsync(options);

            DatabaseModelBuilder builder = new DatabaseModelBuilder();

            builder.Model.DefaultSchema = "public";

            foreach (var type in this.GetTypeMappings())
                builder.Model.Types.Add(type);

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

        private async Task AddTablesAndColumnAsync(DatabaseModelBuilder builder, DbCommand command)
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

        private async Task AddPrimaryKeysAsync(DatabaseModelBuilder builder, DbCommand command)
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

        public async Task AddForeignKeysAsync(DatabaseModelBuilder builder, DbCommand command)
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

        private IEnumerable<TypeModel> GetTypeMappings()
        {
            yield return new TypeModel("boolean", "bool", true);
            yield return new TypeModel("smallint", "short", true);
            yield return new TypeModel("integer", "int", true);
            yield return new TypeModel("bigint", "long", true);
            yield return new TypeModel("real", "float", true);
            yield return new TypeModel("double precision", "double", true);
            yield return new TypeModel("numeric", "decimal", true);
            yield return new TypeModel("money", "decimal", true);
            yield return new TypeModel("text", "string", false);
            yield return new TypeModel("character varying", "string", false);
            yield return new TypeModel("character", "string", false);
            yield return new TypeModel("citext", "string", false);
            yield return new TypeModel("json", "string", false);
            yield return new TypeModel("jsonb", "string", false);
            yield return new TypeModel("xml", "string", false);
            yield return new TypeModel("name", "string", false);
            yield return new TypeModel("bit", "System.Collections.BitArray", false);
            yield return new TypeModel("hstore", "System.Collections.IDictionary<string, string>", false);
            yield return new TypeModel("uuid", "Guid", true);
            yield return new TypeModel("cidr", "(System.Net.IPAddress, int)", true);
            yield return new TypeModel("inet", "System.Net.IPAddress", false);
            yield return new TypeModel("macaddr", "System.Net.NetworkInformation.PhysicalAddress", true);
            yield return new TypeModel("date", "DateTime", true);
            yield return new TypeModel("timestamp", "DateTime", true);
            yield return new TypeModel("timestamp with time zone", "DateTimeOffset", true);
            yield return new TypeModel("timestamp without time zone", "DateTime", true);
            yield return new TypeModel("time", "TimeSpan", true);
            yield return new TypeModel("time with time zone", "DateTimeOffset", true);
            yield return new TypeModel("time without time zone", "TimeSpan", true);
            yield return new TypeModel("interval", "TimeSpan", true);
            yield return new TypeModel("bytea", "byte[]", false);
            yield return new TypeModel("oid", "uint", true);
            yield return new TypeModel("xid", "uint", true);
            yield return new TypeModel("cid", "uint", true);
            yield return new TypeModel("oidvector", "uint[]", false);
            yield return new TypeModel("ARRAY", "Array", false);
        }
    }
}
