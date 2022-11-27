using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.CodeAnalysis;
using Jerrycurl.Text;
using Jerrycurl.Tools.Orm.Model;
using static Jerrycurl.Tools.Orm.Model.DatabaseModel;
using Jerrycurl.Collections;

namespace Jerrycurl.Tools.Orm
{
    public abstract class OrmTool
    {
        protected abstract DbConnection GetConnection(OrmToolOptions options);
        protected abstract Task<DatabaseModel> GetDatabaseModelAsync(OrmToolOptions options, CancellationToken cancellationToken = default);

        public async Task<DatabaseModel> BuildAsync(OrmToolOptions options, CancellationToken cancellationToken = default)
        {
            OrmTransformer transformer = new OrmTransformer();
            DatabaseModel database = await this.GetDatabaseModelAsync(options, cancellationToken);

            this.CreateDefaultClrModel(options, database);

            database = await transformer.TransformAsync(options, database);

            return database;
        }

        public async Task BuildAndOutputAsync(OrmToolOptions options, string outputPath = null, CancellationToken cancellationToken = default)
        {
            OrmCodeWriter codeWriter = new OrmCodeWriter();
            DatabaseModel database = await this.BuildAsync(options, cancellationToken);

            await codeWriter.WriteAsync(database, outputPath ?? options.Output);
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

        private void CreateDefaultClrModel(OrmToolOptions options, DatabaseModel database)
        {
            database.Imports ??= new List<string>();
            database.Imports.Add("global::System");
            database.Imports.Add("global::Jerrycurl.Cqs.Metadata.Annotations");
            database.Imports.Add("global::Jerrycurl.Mvc.Metadata.Annotations");

            string defaultSchema = database.Flags?.GetValueOrDefault("defaultSchema");
            bool useNullables = (options.Flags?.GetValueOrDefault("useNullables") == "true");

            foreach (TableModel table in database.Tables)
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
                TypeModel mapping = database.Types?.FirstOrDefault(t => t.DbName.Equals(column.TypeName, StringComparison.OrdinalIgnoreCase));

                if (mapping != null)
                    return ((mapping.IsNullable || useNullables) && column.IsNullable) ? mapping.ClrName + "?" : mapping.ClrName;

                return column.TypeName;
            }
        }

        public async Task<DbConnection> OpenConnectionAsync(OrmToolOptions options)
        {
            DbConnection connection = this.GetConnection(options);

            if (connection == null)
                throw new Exception("Connection returned null.");

            try
            {
                connection.ConnectionString = options.Connection;
            }
            catch (Exception ex)
            {
                connection.Dispose();

                throw new Exception("Invalid connection string: " + ex.Message, ex);
            }

            //if (!string.IsNullOrEmpty(connection.Database))
            //    DotNetJerryHostV2.WriteLine($"Connecting to '{connection.Database}'...", ConsoleColor.Yellow);
            //else
            //    DotNetJerryHostV2.WriteLine("Connecting to database...", ConsoleColor.Yellow);

            try
            {
                await connection.OpenAsync().ConfigureAwait(false);

                return connection;
            }
            catch (Exception ex)
            {
                connection.Dispose();

                throw new Exception("Unable to open connection: " + ex.Message, ex);
            }
        }


    }
}
