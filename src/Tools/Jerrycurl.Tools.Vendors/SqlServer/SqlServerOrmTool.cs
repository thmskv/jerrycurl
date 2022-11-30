using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Tools.Orm;
using Jerrycurl.Tools.Orm.Model;
#if NET20_BASE
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif

namespace Jerrycurl.Tools.Vendors.SqlServer
{
    public class SqlServerOrmTool : OrmTool
    {
        protected override DbConnection GetConnection(OrmToolOptions options) => new SqlConnection(options.Connection);

        protected override async Task<SchemaModel> BuildSchemaAsync(DbConnection connection, SchemaBuilder builder, CancellationToken cancellationToken = default)
        {
            this.AddTypeMappings(builder);

            builder.SetFlag("defaultSchema", "dbo", overwrite: false);

            using (DbCommand tablesAndColumns = connection.CreateCommand())
            {
                tablesAndColumns.CommandText = @"SELECT *,
                                                        COLUMNPROPERTY(OBJECT_ID(T2.TABLE_SCHEMA + '.' + T2.TABLE_NAME), T2.COLUMN_NAME, 'IsIdentity') AS IS_IDENTITY
                                                 FROM INFORMATION_SCHEMA.TABLES T1
                                                 INNER JOIN INFORMATION_SCHEMA.COLUMNS T2 ON T2.TABLE_SCHEMA = T1.TABLE_SCHEMA AND T2.TABLE_NAME = T1.TABLE_NAME
                                                 WHERE T1.TABLE_TYPE = 'BASE TABLE'
                                                 ORDER BY T1.TABLE_SCHEMA, T1.TABLE_NAME, T2.ORDINAL_POSITION";

                await this.AddTablesAndColumnsAsync(builder, tablesAndColumns);
            }

            using (DbCommand tableTypesAndColumns = connection.CreateCommand())
            {
                tableTypesAndColumns.CommandText = @"SELECT
	                                                    T4.name		AS TABLE_SCHEMA,
	                                                    T1.name     AS TABLE_NAME,
	                                                    T2.name     AS COLUMN_NAME,
	                                                    T3.name     AS DATA_TYPE,
	                                                    CASE T2.is_nullable
		                                                    WHEN 1 THEN 'YES'
		                                                    ELSE 'NO'
	                                                    END AS IS_NULLABLE,
	                                                    0 AS IS_IDENTITY
                                                    FROM		sys.table_types T1
                                                    INNER JOIN	sys.columns T2
	                                                    ON T2.object_id = T1.type_table_object_id
                                                     INNER JOIN	sys.systypes T3
	                                                    ON T3.xusertype = T2.system_type_id AND T3.uid = 4
                                                    INNER JOIN	sys.schemas T4
	                                                    ON T4.schema_id = T1.schema_id
                                                    ORDER BY T1.name, T2.column_id";

                await this.AddTablesAndColumnsAsync(builder, tableTypesAndColumns);
            }

            using (DbCommand primaryKeys = connection.CreateCommand())
            {
                primaryKeys.CommandText = @"SELECT T2.* FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS T1
                                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE T2 ON T2.CONSTRAINT_CATALOG = T1.CONSTRAINT_CATALOG AND T2.CONSTRAINT_SCHEMA = T1.CONSTRAINT_SCHEMA AND T2.CONSTRAINT_NAME = T1.CONSTRAINT_NAME
                                            WHERE T1.CONSTRAINT_TYPE IN ('PRIMARY KEY', 'UNIQUE')
                                            ORDER BY T2.ORDINAL_POSITION";

                await this.AddPrimaryKeysAsync(builder, primaryKeys);
            }

            using (DbCommand foreignKeys = connection.CreateCommand())
            {
                foreignKeys.CommandText = @"SELECT T2.*, T1.UNIQUE_CONSTRAINT_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS T1
                                            INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS T2  ON T2.CONSTRAINT_CATALOG = T1.CONSTRAINT_CATALOG  AND T2.CONSTRAINT_SCHEMA = T1.CONSTRAINT_SCHEMA AND T2.CONSTRAINT_NAME = T1.CONSTRAINT_NAME
                                            ORDER BY T2.ORDINAL_POSITION";

                await this.AddForeignKeysAsync(builder, foreignKeys);
            }

            return builder.Model;
        }

        private async Task AddTablesAndColumnsAsync(SchemaBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableSchema = tuple["TABLE_SCHEMA"] as string;
                string tableName = tuple["TABLE_NAME"] as string;
                string columnName = tuple["COLUMN_NAME"] as string;
                string typeName = tuple["DATA_TYPE"] as string;
                bool isNullable = (tuple["IS_NULLABLE"] as string == "YES");
                bool isIdentity = ((int?)tuple["IS_IDENTITY"] == 1);
                bool ignoreTable = this.IsIgnoredTable(tableSchema, tableName);

                builder.AddColumn(tableSchema, tableName, columnName, typeName, isNullable: isNullable, isIdentity: isIdentity, ignoreTable: ignoreTable);
            }
        }

        private async Task AddPrimaryKeysAsync(SchemaBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableSchema = tuple["TABLE_SCHEMA"] as string;
                string tableName = tuple["TABLE_NAME"] as string;
                string columnName = tuple["COLUMN_NAME"] as string;
                string keyName = tuple["CONSTRAINT_NAME"] as string;
                int keyIndex = int.Parse(tuple["ORDINAL_POSITION"]?.ToString());

                builder.AddKey(tableSchema, tableName, columnName, keyName, keyIndex);
            }
        }

        public async Task AddForeignKeysAsync(SchemaBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableSchema = tuple["TABLE_SCHEMA"] as string;
                string tableName = tuple["TABLE_NAME"] as string;
                string columnName = tuple["COLUMN_NAME"] as string;
                string uniqueName = tuple["UNIQUE_CONSTRAINT_NAME"] as string;
                string foreignName = tuple["CONSTRAINT_NAME"] as string;
                int keyIndex = int.Parse(tuple["ORDINAL_POSITION"]?.ToString());

                builder.AddReference(tableSchema, tableName, columnName, foreignName, uniqueName, keyIndex);
            }
        }

        private bool IsIgnoredTable(string tableSchema, string tableName)
        {
            if (tableSchema == "dbo" && tableName == "sysdiagrams")
                return true;

            return false;
        }

        private void AddTypeMappings(SchemaBuilder builder)
        {
            builder.AddType("int", "int", true);
            builder.AddType("bigint", "long", true);
            builder.AddType("smallint", "short", true);
            builder.AddType("tinyint", "byte", true);
            builder.AddType("bit", "bool", true);
            builder.AddType("date", "DateTime", true);
            builder.AddType("datetime", "DateTime", true);
            builder.AddType("datetime2", "DateTime", true);
            builder.AddType("smalldatetime", "DateTime", true);
            builder.AddType("time", "TimeSpan", true);
            builder.AddType("datetimeoffset", "DateTimeOffset", true);
            builder.AddType("nvarchar", "string", false);
            builder.AddType("varchar", "string", false);
            builder.AddType("text", "string", false);
            builder.AddType("ntext", "string", false);
            builder.AddType("char", "string", false);
            builder.AddType("nchar", "string", false);
            builder.AddType("varbinary", "byte[]", false);
            builder.AddType("binary", "byte[]", false);
            builder.AddType("image", "byte[]", false);
            builder.AddType("smallmoney", "decimal", true);
            builder.AddType("money", "decimal", true);
            builder.AddType("decimal", "decimal", true);
            builder.AddType("numeric", "decimal", true);
            builder.AddType("real", "float", true);
            builder.AddType("float", "double", true);
            builder.AddType("uniqueidentifier", "Guid", true);
            builder.AddType("geography", "Microsoft.SqlServer.Types.SqlGeography", false);
            builder.AddType("geometry", "Microsoft.SqlServer.Types.SqlGeometry", false);
            builder.AddType("hierarchyid", "Microsoft.SqlServer.Types.SqlHierarchyId", true);
            builder.AddType("sql_variant", "object", false);
            builder.AddType("xml", "System.Xml.Linq.XDocument", false);
        }
    }
}
