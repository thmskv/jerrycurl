using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using System.Linq;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Jerrycurl.Tools.Orm;
using static Jerrycurl.Tools.Orm.Model.DatabaseModel;
using Jerrycurl.Tools.Orm.Model;

namespace Jerrycurl.Tools.Vendors.Sqlite
{
    public class SqliteOrmTool : OrmTool
    {
        protected override DbConnection GetConnection(OrmToolOptions options) => new SqliteConnection();
        protected override async Task<DatabaseModel> GetDatabaseModelAsync(OrmToolOptions options, CancellationToken cancellationToken = default)
        {
            DbConnection connection = await this.OpenConnectionAsync(options);
            DatabaseModelBuilder builder = new DatabaseModelBuilder();

            foreach (TypeModel type in this.GetTypeMappings())
                builder.Model.Types.Add(type);

            using (DbCommand tablesAndColumns = connection.CreateCommand())
            {
                string systemTables = string.Join(",", this.GetSystemTableNames().Select(t => $"'{t}'"));

                if (this.HasSequenceTable(connection))
                {
                    tablesAndColumns.CommandText = @"SELECT m.name AS tbl_name, ti.*, m.sql,
                                                    (SELECT COUNT(*) FROM sqlite_sequence s WHERE s.name = m.name) AS autoincr
                                                    FROM sqlite_master AS m
                                                    JOIN pragma_table_info(m.name) AS ti
                                                    WHERE m.type = 'table'
                                                    ORDER BY m.name, ti.cid";
                }
                else
                {
                    tablesAndColumns.CommandText = @"SELECT m.name AS tbl_name, ti.*, 0 AS autoincr, m.sql
                                                    FROM sqlite_master AS m
                                                    JOIN pragma_table_info(m.name) AS ti
                                                    WHERE m.type = 'table'
                                                    ORDER BY m.name, ti.cid";
                }


                await this.AddTablesAndColumnsAsync(builder, tablesAndColumns);
            }

            using (DbCommand foreignKeys = connection.CreateCommand())
            {
                foreignKeys.CommandText = $@"SELECT m.tbl_name, fks.*
                                             FROM sqlite_master AS m
                                             JOIN pragma_foreign_key_list(m.name) AS fks
                                             WHERE m.type = 'table'
                                             ORDER BY fks.seq";

                await this.AddForeignKeysAsync(builder, foreignKeys);
            }



            return builder.Model;
        }

        private bool HasSequenceTable(DbConnection connection)
        {
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='sqlite_sequence'";

                return (command.ExecuteScalar() != null);
            }
        }

        private async Task AddTablesAndColumnsAsync(DatabaseModelBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string sqlDef = tuple["sql"] as string;

                string tableName = tuple["tbl_name"] as string;
                string columnName = tuple["name"] as string;
                string typeName = this.GetNormalizedTypeName(tuple);
                int keyIndex = (int)(long)tuple["pk"];
                bool isAutoIncrement = keyIndex > 0 && ((long)tuple["autoincr"] > 0 || this.HasAutoIncrementInSqlDefinition(columnName, sqlDef));
                bool isNullable = (keyIndex == 0 && (long)tuple["notnull"] == 0);
                bool ignoreTable = this.IsIgnoredTable(tableName);

                builder.AddColumn(null, tableName, columnName, typeName, isNullable, isIdentity: isAutoIncrement, ignoreTable: ignoreTable);

                if (keyIndex > 0)
                    builder.AddKey(null, tableName, columnName, "pk_" + tableName, (int)keyIndex);
            }
        }

        public async Task AddForeignKeysAsync(DatabaseModelBuilder builder, DbCommand command)
        {
            await foreach (TupleModel tuple in this.QueryAsync(command))
            {
                string tableName = tuple["tbl_name"] as string;
                string columnName = tuple["from"] as string;
                string uniqueName = $"pk_{tuple["table"]}";
                string foreignName = $"fk_{tableName}_{tuple["table"]}_{tuple["id"]}";
                int keyIndex = (int)(long)tuple["seq"] + 1;

                builder.AddReference(null, tableName, columnName, foreignName, uniqueName, keyIndex);
            }
        }

        private bool IsIgnoredTable(string tableName) => this.GetSystemTableNames().Contains(tableName);

        private IEnumerable<string> GetSystemTableNames()
        {
            return new[]
            {
                "ElementaryGeometries",
                "geometry_columns",
                "geometry_columns_auth",
                "geometry_columns_field_infos",
                "geometry_columns_statistics",
                "geometry_columns_time",
                "spatial_ref_sys",
                "spatial_ref_sys_aux",
                "SpatialIndex",
                "spatialite_history",
                "sql_statements_log",
                "views_geometry_columns",
                "views_geometry_columns_auth",
                "views_geometry_columns_field_infos",
                "views_geometry_columns_statistics",
                "virts_geometry_columns",
                "virts_geometry_columns_auth",
                "geom_cols_ref_sys",
                "spatial_ref_sys_all",
                "virts_geometry_columns_field_infos",
                "virts_geometry_columns_statistics",
                "sqlite_sequence",
                "sqlite_stat1",
            };
        }

        private string GetUnpingedNameFromDefinition(string def)
        {
            if (def.Length == 0)
                return "";

            def = def.Trim();

            char[] pings = new[] { '"', '\'', '`' };

            foreach (char ping in pings)
            {
                int i2 = def.IndexOf(ping, 1);

                if (def[0] == ping && i2 > -1)
                    return def.Substring(1, i2 - 1);
            }

            int i3 = def.Count(c => !char.IsWhiteSpace(c));

            return def.Substring(0, i3);
        }

        private bool HasAutoIncrementInSqlDefinition(string columnName, string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return false;

            int i1 = sql.IndexOf('(');
            int i2 = sql.LastIndexOf(')');

            if (i1 > -1 && i2 > i1)
            {
                string def = sql.Substring(i1 + 1, i2 - i1 - 1);

                foreach (string colDef in def.Split(','))
                {
                    string matchName = this.GetUnpingedNameFromDefinition(colDef);

                    if (matchName.Equals(columnName, StringComparison.OrdinalIgnoreCase) && Regex.IsMatch(colDef, @"INTEGER\s+PRIMARY\s+KEY\s+AUTOINCREMENT"))
                        return true;
                }
            }

            return false;
        }

        private string GetNormalizedTypeName(TupleModel tuple)
        {
            if (tuple["type"] is string typeName)
            {
                int sizeIndex = typeName.IndexOf('(');

                if (sizeIndex == -1)
                    return typeName;

                return typeName.Remove(sizeIndex);
            }

            return null;
        }

        private IEnumerable<TypeModel> GetTypeMappings()
        {
            // INTEGER affinity
            yield return new TypeModel("int", "int", true);
            yield return new TypeModel("integer", "int", true);
            yield return new TypeModel("tinyint", "byte", true);
            yield return new TypeModel("smallint", "short", true);
            yield return new TypeModel("mediumint", "int", true);
            yield return new TypeModel("bigint", "long", true);
            yield return new TypeModel("unsigned big int", "ulong", true);
            yield return new TypeModel("int2", "short", true);
            yield return new TypeModel("int4", "int", true);
            yield return new TypeModel("int8", "long", true);

            // TEXT affinity
            yield return new TypeModel("character", "string", false);
            yield return new TypeModel("varchar", "string", false);
            yield return new TypeModel("varying character", "string", false);
            yield return new TypeModel("nchar", "string", false);
            yield return new TypeModel("native character", "string", false);
            yield return new TypeModel("nvarchar", "string", false);
            yield return new TypeModel("text", "string", false);
            yield return new TypeModel("clob", "string", false);

            // BLOB affinity
            yield return new TypeModel("blob", "byte[]", false);

            // FLOATING 
            yield return new TypeModel("float", "float", false);
            yield return new TypeModel("real", "float", false);
            yield return new TypeModel("double", "double", true);
            yield return new TypeModel("double precision", "double", true);

            // NUMERIC
            yield return new TypeModel("numeric", "decimal", true);
            yield return new TypeModel("decimal", "decimal", true);
            yield return new TypeModel("boolean", "bool", true);
            yield return new TypeModel("datetime", "DateTime", true);
            yield return new TypeModel("date", "DateTime", true);
        }
    }
}
