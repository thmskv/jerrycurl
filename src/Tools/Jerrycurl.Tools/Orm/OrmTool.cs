using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.CodeAnalysis;
using Jerrycurl.Text;
using Jerrycurl.Tools.Orm.Model;
using static Jerrycurl.Tools.Orm.Model.SchemaModel;
using Jerrycurl.Collections;

namespace Jerrycurl.Tools.Orm
{
    public abstract class OrmTool
    {
        protected abstract DbConnection GetConnection(OrmToolOptions options);
        protected abstract Task BuildSchemaAsync(DbConnection connection, SchemaBuilder builder, CancellationToken cancellationToken = default);

        public async Task<SchemaModel> BuildAsync(OrmToolOptions options, ToolConsole console, CancellationToken cancellationToken = default)
        {
            using DbConnection connection = await this.OpenConnectionAsync(options, console);

            return await console.RunAsync("Building schema", async () =>
            {
                SchemaBuilder builder = new SchemaBuilder(options);
                OrmTransformer transformer = new OrmTransformer();

                await this.BuildSchemaAsync(connection, builder, cancellationToken);

                this.CreateDefaultClrModel(options, builder.Model);

                SchemaModel transformed = await transformer.TransformAsync(options, builder.Model);

                return transformed;
            });
        }

        public async Task WriteAsync(SchemaModel schema, OrmToolOptions options, ToolConsole console, string outputPath = null, CancellationToken cancellationToken = default)
        {
            await console.RunAsync("Writing to file", async () =>
            {
                OrmCodeWriter codeWriter = new OrmCodeWriter();

                await codeWriter.WriteAsync(schema, outputPath ?? options.Output, console);
            });
        }

        public async IAsyncEnumerable<TupleModel> QueryAsync(DbCommand command)
        {
            using (DbDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
            {
                do
                {
                    while (await reader.ReadAsync().ConfigureAwait(false))
                        yield return new TupleModel(reader);
                }
                while (await reader.NextResultAsync().ConfigureAwait(false));
            }
        }

        private void CreateDefaultClrModel(OrmToolOptions options, SchemaModel schema)
        {
            schema.Imports ??= new List<string>();
            schema.Imports.Add("global::System");
            schema.Imports.Add("global::Jerrycurl.Cqs.Metadata.Annotations");
            schema.Imports.Add("global::Jerrycurl.Mvc.Metadata.Annotations");

            string defaultSchema = schema.Flags?.GetValueOrDefault("defaultSchema");
            bool useNullables = (options.Flags?.GetValueOrDefault("useNullables") != "false");

            foreach (TableModel table in schema.Tables)
            {
                table.Clr = new ClassModel()
                {
                    Modifiers = new[] { "public" },
                    Name = CSharp.Identifier(table.Name),
                    Namespace = GetNamespace(table),
                };

                foreach (ColumnModel column in table.Columns)
                {
                    column.Clr = new PropertyModel()
                    {
                        Modifiers = new[] { "public" },
                        TypeName = GetColumnTypeName(column),
                        Name = CSharp.Identifier(column.Name),
                    };

                    if (column.Clr.Name.Equals(table.Clr.Name))
                        column.Clr.Name += "0";
                }
            }

            string GetNamespace(TableModel table)
            {
                Namespace ns = new Namespace(options.Namespace ?? "Database");

                if (!string.IsNullOrEmpty(table.Schema) && !table.Schema.Equals(defaultSchema))
                    ns = ns.Add(table.Schema.ToCapitalCase());

                return ns.Definition;
            }

            string GetColumnTypeName(ColumnModel column)
            {
                TypeModel mapping = schema.Types?.FirstOrDefault(t => t.DbName.Equals(column.TypeName, StringComparison.OrdinalIgnoreCase));

                if (mapping != null)
                    return ((mapping.IsNullable || useNullables) && column.IsNullable) ? mapping.ClrName + "?" : mapping.ClrName;

                return column.TypeName;
            }
        }

        public async Task<DbConnection> OpenConnectionAsync(OrmToolOptions options, ToolConsole console)
        {
            DbConnection connection = await console.RunAsync("Preparing database", async () =>
            {
                DbConnection connection = this.GetConnection(options);

                if (connection == null)
                    throw new ToolException(OrmErrorCodes.InvalidConnection, "Connection returned null.");

                try
                {
                    connection.ConnectionString = options.Connection;
                }
                catch (Exception ex)
                {
                    connection.Dispose();

                    throw new ToolException(OrmErrorCodes.InvalidConnection, $"Invalid connection string '{connection.ConnectionString}': {ex.Message}", innerException: ex);
                }

                return connection;
            });

            bool hasDatabase = !string.IsNullOrEmpty(connection.Database);
            string connectText = hasDatabase ? $"Connecting to database '{connection.Database}'" : "Connecting to database";

            return await console.RunAsync(connectText, async () =>
            {
                try
                {
                    await connection.OpenAsync().ConfigureAwait(false);

                    return connection;
                }
                catch (Exception ex)
                {
                    connection.Dispose();

                    throw new ToolException(OrmErrorCodes.ConnectionFailed, $"Cannot connect to database: {ex.Message}", innerException: ex);
                }
            });
        }
    }
}
