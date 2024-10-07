using Jerrycurl.Tools.Orm;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Tools.Orm.Model;

namespace Jerrycurl.Tools.Vendors.Oracle;

public class OracleOrmTool : OrmTool
{
    protected override DbConnection GetConnection(OrmToolOptions options) => new OracleConnection(options.Connection);

    protected override async Task<SchemaModel> BuildSchemaAsync(DbConnection connection, SchemaBuilder builder, CancellationToken cancellationToken = default)
    {
        this.AddTypeMappings(builder);

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

    private async Task AddTablesAndColumnAsync(SchemaBuilder builder, DbCommand command)
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

    private async Task AddPrimaryKeysAsync(SchemaBuilder builder, DbCommand command)
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

    public async Task AddForeignKeysAsync(SchemaBuilder builder, DbCommand command)
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

    private void AddTypeMappings(SchemaBuilder builder)
    {
        builder.AddType("BFILE", "byte[]", false);
        builder.AddType("BLOB", "byte[]", false);
        builder.AddType("CHAR", "string", false);
        builder.AddType("CLOB", "string", false);
        builder.AddType("DATE", "DateTime", true);
        builder.AddType("FLOAT", "decimal", true);
        builder.AddType("INTEGER", "decimal", true);
        builder.AddType("INTERVAL YEAR TO MONTH", "long", true);
        builder.AddType("INTERVAL DAY TO SECOND", "TimeSpan", true);
        builder.AddType("LONG", "string", false);
        builder.AddType("LONG RAW", "byte[]", false);
        builder.AddType("NCHAR", "string", false);
        builder.AddType("NCLOB", "string", false);
        builder.AddType("NUMBER", "decimal", true);
        builder.AddType("NVARCHAR2", "string", false);
        builder.AddType("RAW", "byte[]", false);
        builder.AddType("ROWID", "string", false);
        builder.AddType("TIMESTAMP", "DateTime", true);
        builder.AddType("TIMESTAMP WITH LOCAL TIME ZONE", "DateTime", true);
        builder.AddType("TIMESTAMP WITH TIME ZONE", "DateTimeOffset", true);
        builder.AddType("UNSIGNED INTEGER", "decimal", true);
        builder.AddType("VARCHAR2", "string", false);
        builder.AddType("ANYDATA", "object", false);
    }
}
