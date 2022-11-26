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

namespace Jerrycurl.Tools.Orm
{
    public abstract class OrmCommand
    {
        public abstract DbConnection GetDbConnection();
        public abstract Task<DatabaseModel> GetDatabaseModelAsync(DbConnection connection, CancellationToken cancellationToken = default);
        public abstract IEnumerable<TypeModel> GetTypeMappings();

        public void ApplyDefaults(OrmModel ormFile, DatabaseModel databaseModel, IList<TypeModel> typeMappings)
        {
            databaseModel.Imports = new List<string>()
            {
                "global::System",
                "global::Jerrycurl.Cqs.Metadata.Annotations",
                "global::Jerrycurl.Mvc.Metadata.Annotations",
            };

            foreach (DatabaseModel.TableModel table in databaseModel.Tables)
            {
                table.Clr = new DatabaseModel.ClassModel()
                {
                    Modifiers = new[] { "public" },
                    Name = CSharp.Identifier(table.Name),
                    Namespace = this.GetNamespace(ormFile, table, databaseModel.DefaultSchema),
                };

                foreach (DatabaseModel.ColumnModel column in table.Columns)
                {
                    column.Clr = new DatabaseModel.PropertyModel()
                    {
                        Modifiers = new[] { "public" },
                        TypeName = this.GetColumnTypeName(column, typeMappings),
                        Name = CSharp.Identifier(column.Name),
                    };

                    if (column.Clr.Name.Equals(table.Clr.Name))
                        column.Clr.Name += "0";
                }
            }
        }

        private string GetNamespace(OrmModel ormFile, DatabaseModel.TableModel table, string defaultSchema)
        {
            Namespace ns = new Namespace(ormFile.Namespace ?? "Database");

            if (!string.IsNullOrEmpty(table.Schema) && !table.Schema.Equals(defaultSchema))
                ns = ns.Add(table.Schema.ToCapitalCase());

            return ns.Definition;
        }

        private string GetColumnTypeName(DatabaseModel.ColumnModel column, IList<TypeModel> typeMappings)
        {
            TypeModel mapping = typeMappings?.FirstOrDefault(t => t.DbName.Equals(column.TypeName, StringComparison.OrdinalIgnoreCase));

            if (mapping != null)
                return (mapping.IsValueType && column.IsNullable) ? mapping.ClrName + "?" : mapping.ClrName;

            return column.TypeName;
        }
    }
}
