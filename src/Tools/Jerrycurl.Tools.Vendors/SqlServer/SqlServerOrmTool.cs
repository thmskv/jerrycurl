using System.Collections.Generic;
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
using TypeModel = Jerrycurl.Tools.Orm.Model.DatabaseModel.TypeModel;

namespace Jerrycurl.Tools.Vendors.SqlServer
{
    public class SqlServerOrmTool : OrmTool
    {
        protected override DbConnection GetConnection(OrmToolOptions options) => new SqlConnection(options.Connection);

        protected override async Task<DatabaseModel> GetDatabaseModelAsync(OrmToolOptions options, CancellationToken cancellationToken = default)
        {
            await using DbConnection connection = this.GetConnection(options);

            await connection.OpenAsync();

            DatabaseModelBuilder builder = new DatabaseModelBuilder();

            foreach (var type in this.GetTypeMappings())
                builder.Model.Types.Add(type);

            builder.Model.DefaultSchema = "dbo";

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

        private async Task AddTablesAndColumnsAsync(DatabaseModelBuilder builder, DbCommand command)
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

        private async Task AddPrimaryKeysAsync(DatabaseModelBuilder builder, DbCommand command)
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

        public async Task AddForeignKeysAsync(DatabaseModelBuilder builder, DbCommand command)
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

        private IEnumerable<TypeModel> GetTypeMappings()
        {
            yield return new TypeModel("int", "int", true);
            yield return new TypeModel("bigint", "long", true);
            yield return new TypeModel("smallint", "short", true);
            yield return new TypeModel("tinyint", "byte", true);
            yield return new TypeModel("bit", "bool", true);
            yield return new TypeModel("date", "DateTime", true);
            yield return new TypeModel("datetime", "DateTime", true);
            yield return new TypeModel("datetime2", "DateTime", true);
            yield return new TypeModel("smalldatetime", "DateTime", true);
            yield return new TypeModel("time", "TimeSpan", true);
            yield return new TypeModel("datetimeoffset", "DateTimeOffset", true);
            yield return new TypeModel("nvarchar", "string", false);
            yield return new TypeModel("varchar", "string", false);
            yield return new TypeModel("text", "string", false);
            yield return new TypeModel("ntext", "string", false);
            yield return new TypeModel("char", "string", false);
            yield return new TypeModel("nchar", "string", false);
            yield return new TypeModel("varbinary", "byte[]", false);
            yield return new TypeModel("binary", "byte[]", false);
            yield return new TypeModel("image", "byte[]", false);
            yield return new TypeModel("smallmoney", "decimal", true);
            yield return new TypeModel("money", "decimal", true);
            yield return new TypeModel("decimal", "decimal", true);
            yield return new TypeModel("numeric", "decimal", true);
            yield return new TypeModel("real", "float", true);
            yield return new TypeModel("float", "double", true);
            yield return new TypeModel("uniqueidentifier", "Guid", true);
            yield return new TypeModel("geography", "Microsoft.SqlServer.Types.SqlGeography", false);
            yield return new TypeModel("geometry", "Microsoft.SqlServer.Types.SqlGeometry", false);
            yield return new TypeModel("hierarchyid", "Microsoft.SqlServer.Types.SqlHierarchyId", true);
            yield return new TypeModel("sql_variant", "object", false);
            yield return new TypeModel("xml", "System.Xml.Linq.XDocument", false);
        }
    }
}
