using Jerrycurl.Tools.Orm;
using MySql.Data.MySqlClient;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Tools.Orm.Model;

namespace Jerrycurl.Tools.Vendors.MySql;

public class MySqlOrmTool : OrmTool
{
    protected override DbConnection GetConnection(OrmToolOptions options) => new MySqlConnection(options.Connection);

    protected override async Task<SchemaModel> BuildSchemaAsync(DbConnection connection, SchemaBuilder builder, CancellationToken cancellationToken = default)
    {
        this.AddTypeMappings(builder);

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

    private async Task AddTablesAndColumnsAsync(SchemaBuilder builder, DbCommand command)
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

    private async Task AddPrimaryKeysAsync(SchemaBuilder builder, DbCommand command)
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

    public async Task AddForeignKeysAsync(SchemaBuilder builder, DbCommand command)
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

    private void AddTypeMappings(SchemaBuilder builder)
    {
        builder.AddType("bigint", "long", true);
        builder.AddType("decimal", "decimal", true);
        builder.AddType("double", "double", true);
        builder.AddType("float", "float", true);
        builder.AddType("real", "float", true);
        builder.AddType("int", "int", true);
        builder.AddType("mediumint", "int", true);
        builder.AddType("smallint", "short", true);
        builder.AddType("year", "short", true);
        builder.AddType("bit", "bool", true);
        builder.AddType("tinyint", "byte", true);
        builder.AddType("char", "char", true);
        builder.AddType("varchar", "string", false);
        builder.AddType("tinytext", "string", false);
        builder.AddType("text", "string", false);
        builder.AddType("mediumtext", "string", false);
        builder.AddType("longtext", "string", false);
        builder.AddType("string", "string", false);
        builder.AddType("enum", "string", false);
        builder.AddType("set", "string", false);
        builder.AddType("json", "string", false);
        builder.AddType("datetime", "DateTime", true);
        builder.AddType("date", "DateTime", true);
        builder.AddType("time", "TimeSpan", true);
        builder.AddType("timestamp", "DateTimeOffset", true);
        builder.AddType("tinyblob", "byte[]", false);
        builder.AddType("blob", "byte[]", false);
        builder.AddType("mediumblob", "byte[]", false);
        builder.AddType("longblob", "byte[]", false);
        builder.AddType("binary", "byte[]", false);
        builder.AddType("varbinary", "byte[]", false);
        builder.AddType("geometry", "byte[]", false);
    }
}
