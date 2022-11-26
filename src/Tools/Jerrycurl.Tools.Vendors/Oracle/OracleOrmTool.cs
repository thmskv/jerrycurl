using Jerrycurl.Tools.Orm;
using static Jerrycurl.Tools.Orm.Model.DatabaseModel;
using Oracle.ManagedDataAccess.Client;
using System.Collections.Generic;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Tools.Orm.Model;

namespace Jerrycurl.Tools.Vendors.Oracle
{
    public class OracleOrmTool : OrmTool
    {
        protected override DbConnection GetConnection(OrmToolOptions options) => new OracleConnection(options.Connection);

        protected override async Task<DatabaseModel> GetDatabaseModelAsync(OrmToolOptions options, CancellationToken cancellationToken = default)
        {
            await using DbConnection connection = await this.OpenConnectionAsync(options);

            DatabaseModelBuilder builder = new DatabaseModelBuilder();

            foreach (var type in this.GetTypeMappings())
                builder.Model.Types.Add(type);

            using (DbCommand tablesAndColumns = connection.CreateCommand())
            {
                tablesAndColumns.CommandText = @"SELECT T2.*
                                                 FROM user_tables T1
                                                 INNER JOIN user_tab_columns T2 ON T2.TABLE_NAME = T1.TABLE_NAME
                                                 ORDER BY T2.TABLE_NAME, T2.COLUMN_ID";

                await this.AddTablesAndColumnAsync(builder, tablesAndColumns);
            }

            using (DbCommand primaryKeys = connection.CreateCommand())
            {
                primaryKeys.CommandText = @"SELECT *
                                            FROM user_cons_columns a
                                            JOIN user_constraints c ON a.constraint_name = c.constraint_name
                                            INNER JOIN user_tables t ON t.table_name = a.table_name
                                            AND c.constraint_type IN ('P')";

                await this.AddPrimaryKeysAsync(builder, primaryKeys);
            }

            using (DbCommand foreignKeys = connection.CreateCommand())
            {
                foreignKeys.CommandText = @"SELECT x.*, c.constraint_name AS unique_constraint_name
                                            FROM all_cons_columns x,
                                                 all_cons_columns c,
                                                 all_constraints r,
                                                 user_tables t
                                                 WHERE x.constraint_name = r.constraint_name
                                                  AND t.table_name = x.table_name
                                                  AND c.constraint_name = r.r_constraint_name
                                                  AND c.owner = r.r_owner
                                                  AND r.constraint_type = 'R'
                                            ORDER BY x.position";

                await this.AddForeignKeysAsync(builder, foreignKeys);
            }

            return builder.Model;
        }

        private async Task AddTablesAndColumnAsync(DatabaseModelBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableSchema = null;
                string tableName = (tuple["TABLE_NAME"] as string).Trim();
                string columnName = tuple["COLUMN_NAME"] as string;
                string typeName = this.GetSanitizedDataType(tuple);
                bool isNullable = (tuple["NULLABLE"] as string == "Y");
                bool isIdentity = (tuple["IDENTITY_COLUMN"] as string == "YES");
                bool ignoreTable = false;

                builder.AddColumn(tableSchema, tableName, columnName, typeName, isNullable: isNullable, isIdentity: isIdentity, ignoreTable: ignoreTable);
            }
        }

        private string GetSanitizedDataType(TupleModel tuple)
        {
            string dataType = (tuple["DATA_TYPE"] as string)?.Trim();

            if (dataType != null)
                dataType = Regex.Replace(dataType, @"\(\d+\)", "");

            return dataType;
        }

        private async Task AddPrimaryKeysAsync(DatabaseModelBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableSchema = null;
                string tableName = tuple["TABLE_NAME"] as string;
                string columnName = tuple["COLUMN_NAME"] as string;
                string keyName = tuple["CONSTRAINT_NAME"] as string;
                int keyIndex = int.Parse(tuple["POSITION"]?.ToString());

                builder.AddKey(tableSchema, tableName, columnName, keyName, keyIndex);
            }
        }

        public async Task AddForeignKeysAsync(DatabaseModelBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableSchema = null;
                string tableName = tuple["TABLE_NAME"] as string;
                string columnName = tuple["COLUMN_NAME"] as string;
                string uniqueName = tuple["UNIQUE_CONSTRAINT_NAME"] as string;
                string foreignName = tuple["CONSTRAINT_NAME"] as string;
                int keyIndex = int.Parse(tuple["POSITION"]?.ToString());

                builder.AddReference(tableSchema, tableName, columnName, foreignName, uniqueName, keyIndex);
            }
        }

        private IEnumerable<TypeModel> GetTypeMappings()
        {
            yield return new TypeModel("BFILE", "byte[]", false);
            yield return new TypeModel("BLOB", "byte[]", false);
            yield return new TypeModel("CHAR", "string", false);
            yield return new TypeModel("CLOB", "string", false);
            yield return new TypeModel("DATE", "DateTime", true);
            yield return new TypeModel("FLOAT", "decimal", true);
            yield return new TypeModel("INTEGER", "decimal", true);
            yield return new TypeModel("INTERVAL YEAR TO MONTH", "long", true);
            yield return new TypeModel("INTERVAL DAY TO SECOND", "TimeSpan", true);
            yield return new TypeModel("LONG", "string", false);
            yield return new TypeModel("LONG RAW", "byte[]", false);
            yield return new TypeModel("NCHAR", "string", false);
            yield return new TypeModel("NCLOB", "string", false);
            yield return new TypeModel("NUMBER", "decimal", true);
            yield return new TypeModel("NVARCHAR2", "string", false);
            yield return new TypeModel("RAW", "byte[]", false);
            yield return new TypeModel("ROWID", "string", false);
            yield return new TypeModel("TIMESTAMP", "DateTime", true);
            yield return new TypeModel("TIMESTAMP WITH LOCAL TIME ZONE", "DateTime", true);
            yield return new TypeModel("TIMESTAMP WITH TIME ZONE", "DateTimeOffset", true);
            yield return new TypeModel("UNSIGNED INTEGER", "decimal", true);
            yield return new TypeModel("VARCHAR2", "string", false);
            yield return new TypeModel("ANYDATA", "object", false);
        }
    }
}
