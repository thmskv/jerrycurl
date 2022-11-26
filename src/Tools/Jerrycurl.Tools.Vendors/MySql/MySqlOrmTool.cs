using Jerrycurl.Tools.Orm;
using static Jerrycurl.Tools.Orm.Model.DatabaseModel;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Jerrycurl.Tools.Orm.Model;

namespace Jerrycurl.Tools.Vendors.MySql
{
    public class MySqlOrmTool : OrmTool
    {
        protected override DbConnection GetConnection(OrmToolOptions options) => new MySqlConnection(options.Connection);
        protected override async Task<DatabaseModel> GetDatabaseModelAsync(OrmToolOptions options, CancellationToken cancellationToken = default)
        {
            await using DbConnection connection = await this.OpenConnectionAsync(options);

            DatabaseModelBuilder builder = new DatabaseModelBuilder();

            foreach (var type in this.GetTypeMappings())
                builder.Model.Types.Add(type);

            using (DbCommand tablesAndColumns = connection.CreateCommand())
            {
                tablesAndColumns.CommandText = @"SELECT *
                                                 FROM INFORMATION_SCHEMA.TABLES T1
                                                 INNER JOIN INFORMATION_SCHEMA.COLUMNS T2 ON T2.TABLE_SCHEMA = T1.TABLE_SCHEMA AND T2.TABLE_NAME = T1.TABLE_NAME
                                                 WHERE T1.TABLE_SCHEMA = DATABASE() AND T1.TABLE_TYPE = 'BASE TABLE'
                                                 ORDER BY T1.TABLE_SCHEMA, T1.TABLE_NAME, T2.ORDINAL_POSITION";

                await this.AddTablesAndColumnsAsync(builder, tablesAndColumns);
            }

            using (DbCommand primaryKeys = connection.CreateCommand())
            {
                primaryKeys.CommandText = @"SELECT T2.* FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS T1
                                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE T2 ON T2.CONSTRAINT_CATALOG = T1.CONSTRAINT_CATALOG AND T2.CONSTRAINT_SCHEMA = T1.CONSTRAINT_SCHEMA AND T2.CONSTRAINT_NAME = T1.CONSTRAINT_NAME
                                            WHERE T1.CONSTRAINT_SCHEMA = DATABASE() AND T1.CONSTRAINT_TYPE = 'PRIMARY KEY'
                                            ORDER BY T2.ORDINAL_POSITION";

                await this.AddPrimaryKeysAsync(builder, primaryKeys);
            }

            using (DbCommand foreignKeys = connection.CreateCommand())
            {
                foreignKeys.CommandText = @"SELECT T2.*, T1.UNIQUE_CONSTRAINT_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS T1
                                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS T2  ON T2.CONSTRAINT_CATALOG = T1.CONSTRAINT_CATALOG  AND T2.CONSTRAINT_SCHEMA = T1.CONSTRAINT_SCHEMA AND T2.CONSTRAINT_NAME = T1.CONSTRAINT_NAME
                                            WHERE T1.CONSTRAINT_SCHEMA = DATABASE()
                                            ORDER BY T2.ORDINAL_POSITION";

                await this.AddForeignKeysAsync(builder, foreignKeys);
            }

            return builder.Model;
        }

        private async Task AddTablesAndColumnsAsync(DatabaseModelBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableName = tuple["TABLE_NAME"] as string;
                string columnName = tuple["COLUMN_NAME"] as string;
                string typeName = tuple["DATA_TYPE"] as string;
                bool isNullable = (tuple["IS_NULLABLE"] as string == "YES");
                bool isIdentity = ((string)tuple["EXTRA"]).Contains("auto_increment");

                builder.AddColumn(null, tableName, columnName, typeName, isNullable: isNullable, isIdentity: isIdentity);
            }
        }

        private async Task AddPrimaryKeysAsync(DatabaseModelBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableName = tuple["TABLE_NAME"] as string;
                string columnName = tuple["COLUMN_NAME"] as string;
                string keyName = $"PK_{tableName}";
                int keyIndex = int.Parse(tuple["ORDINAL_POSITION"]?.ToString());

                builder.AddKey(null, tableName, columnName, keyName, keyIndex);
            }
        }

        public async Task AddForeignKeysAsync(DatabaseModelBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableName = tuple["TABLE_NAME"] as string;
                string columnName = tuple["COLUMN_NAME"] as string;
                string uniqueName = $"PK_{tuple["REFERENCED_TABLE_NAME"]}";
                string foreignName = tuple["CONSTRAINT_NAME"] as string;
                int keyIndex = int.Parse(tuple["ORDINAL_POSITION"]?.ToString());

                builder.AddReference(null, tableName, columnName, foreignName, uniqueName, keyIndex);
            }
        }

        private IEnumerable<TypeModel> GetTypeMappings()
        {
            yield return new TypeModel("bigint", "long", true);
            yield return new TypeModel("decimal", "decimal", true);
            yield return new TypeModel("double", "double", true);
            yield return new TypeModel("float", "float", true);
            yield return new TypeModel("real", "float", true);
            yield return new TypeModel("int", "int", true);
            yield return new TypeModel("mediumint", "int", true);
            yield return new TypeModel("smallint", "short", true);
            yield return new TypeModel("year", "short", true);
            yield return new TypeModel("bit", "bool", true);
            yield return new TypeModel("tinyint", "byte", true);
            yield return new TypeModel("char", "char", true);
            yield return new TypeModel("varchar", "string", false);
            yield return new TypeModel("tinytext", "string", false);
            yield return new TypeModel("text", "string", false);
            yield return new TypeModel("mediumtext", "string", false);
            yield return new TypeModel("longtext", "string", false);
            yield return new TypeModel("string", "string", false);
            yield return new TypeModel("enum", "string", false);
            yield return new TypeModel("set", "string", false);
            yield return new TypeModel("json", "string", false);
            yield return new TypeModel("datetime", "DateTime", true);
            yield return new TypeModel("date", "DateTime", true);
            yield return new TypeModel("time", "TimeSpan", true);
            yield return new TypeModel("timestamp", "DateTimeOffset", true);
            yield return new TypeModel("tinyblob", "byte[]", false);
            yield return new TypeModel("blob", "byte[]", false);
            yield return new TypeModel("mediumblob", "byte[]", false);
            yield return new TypeModel("longblob", "byte[]", false);
            yield return new TypeModel("binary", "byte[]", false);
            yield return new TypeModel("varbinary", "byte[]", false);
            yield return new TypeModel("geometry", "byte[]", false);
        }
    }
}
