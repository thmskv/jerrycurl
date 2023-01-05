using Jerrycurl.CodeAnalysis;
using Jerrycurl.Collections;
using Jerrycurl.IO;
using Jerrycurl.Tools.Orm.Model;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jerrycurl.Tools.Orm
{
    internal class OrmCodeWriter
    {
        public async Task WriteAsync(OrmToolOptions options, SchemaModel schema, string writePath, ToolConsole console)
        {
            PathHelper.EnsureDirectory(writePath);

            using (StreamWriter fileWriter = new StreamWriter(writePath, append: false, Encoding.UTF8))
            {
                CSharpWriter writer = new CSharpWriter(fileWriter);

                string noWarn = schema.Flags?.GetValueOrDefault("noWarn");

                if (!string.IsNullOrWhiteSpace(noWarn))
                    await writer.WritePragmaDirectiveAsync($"warning disable {noWarn}");

                if (schema.Imports.Any())
                {
                    foreach (string import in schema.Imports)
                        await writer.WriteImportAsync(import);

                    await writer.WriteLineAsync();
                }

                foreach (var namespaceGroup in schema.Tables.OrderBy(fg => fg.Clr.Namespace).GroupBy(fg => fg.Clr.Namespace).OrderBy(g => g.Key))
                {
                    string ns = namespaceGroup.Key;

                    if (!string.IsNullOrWhiteSpace(ns))
                        await writer.WriteNamespaceStartAsync(ns);

                    foreach (SchemaModel.TableModel table in namespaceGroup.NotNull().OrderBy(t => t.Schema).ThenBy(t => t.Name))
                    {
                        string warningMessage = this.GetWarningMessage(table);

                        if (warningMessage != null)
                        {
                            await writer.WriteWarningDirectiveAsync($"Table ignored. {warningMessage}");

                            continue;
                        }

                        if (table.Schema == null)
                            writer.AddAttribute("Table", table.Name);
                        else
                            writer.AddAttribute("Table", table.Schema, table.Name);

                        await writer.WriteAttributesAsync();
                        await writer.WriteObjectStartAsync(table.Clr.IsStruct ? "struct" : "class", table.Clr.Name, table.Clr.Modifiers, table.Clr.BaseTypes);

                        await this.WriteColumnsAsync(writer, table);

                        await writer.WriteObjectEndAsync();
                        await writer.WriteLineAsync();
                    }

                    if (!string.IsNullOrWhiteSpace(ns))
                        await writer.WriteNamespaceEndAsync();
                }

                if (!string.IsNullOrWhiteSpace(noWarn))
                    await writer.WritePragmaDirectiveAsync($"warning enable {noWarn}");
            }

            if (options.Verbose)
                this.WriteVerboseOutput(options, schema, console);
            else
                this.WriteOutput(options, schema, console);
        }

        private async Task WriteColumnsAsync(CSharpWriter writer, SchemaModel.TableModel tableModel)
        {
            foreach (SchemaModel.ColumnModel column in tableModel.Columns.NotNull().Where(c => !c.Ignore))
            {
                string warningMessage = this.GetWarningMessage(column);

                if (warningMessage != null)
                {
                    await writer.WriteWarningDirectiveAsync($"Column ignored. {warningMessage}");

                    continue;
                }

                string propName = column.Clr.Name;

                if (propName.Equals(tableModel.Clr.Name))
                    propName = $"{propName}0";

                if (column.Name != column.Clr.Name)
                    writer.AddAttribute("Column", column.Name);

                if (column.IsIdentity)
                    writer.AddAttribute("Id");

                if (column.Clr.IsInput)
                    writer.AddAttribute("In");

                if (column.Clr.IsOutput)
                    writer.AddAttribute("Out");

                if (column.Clr.IsJson)
                    writer.AddAttribute("Json");

                foreach (SchemaModel.KeyModel key in column.Keys)
                    writer.AddAttribute("Key", key.Name, key.Index);

                foreach (SchemaModel.ReferenceModel refModel in column.References)
                {
                    if (refModel.Name != null)
                        writer.AddAttribute("Ref", refModel.KeyName, refModel.KeyIndex, refModel.Name);
                    else
                        writer.AddAttribute("Ref", refModel.KeyName, refModel.KeyIndex);
                }
                    

                await writer.WriteAttributesAsync();
                await writer.WritePropertyAsync(column.Clr.TypeName, column.Clr.Name, column.Clr.Modifiers);
            }
        }

        private string GetWarningMessage(SchemaModel.TableModel tableModel)
        {
            if (tableModel?.Clr?.Name == null)
                return "Invalid model data.";

            return null;
        }

        private string GetWarningMessage(SchemaModel.ColumnModel columnModel)
        {
            if (columnModel?.Clr?.TypeName == null || columnModel?.Clr?.Name == null)
                return "Invalid model data.";

            return null;
        }

        private void WriteOutput(OrmToolOptions options, SchemaModel schema, ToolConsole console)
        {
            string outputName = Path.GetFileName(options.Output);
            int count = schema.Tables.Count(t => !t.Ignore);
            string classMoniker = count + " " + (count == 1 ? "class" : "classes");

            console.WriteLine($"Created {classMoniker} in {outputName}.", ConsoleColor.Green);
        }

        private void WriteVerboseOutput(OrmToolOptions options, SchemaModel schema, ToolConsole console)
        {
            foreach (var table in schema.Tables)
            {
                string tableName = !string.IsNullOrEmpty(table.Schema) ? $"{table.Schema}.{table.Name}" : $"{table.Name}";
                string className = !string.IsNullOrEmpty(table.Clr.Namespace) ? $"{table.Clr.Namespace}.{table.Clr.Name}" : table.Clr.Name;
                string[] propertyNames = table.Columns.Where(c => !c.Ignore).Select(c => $"{c.Clr.Name}").ToArray();
                string propertyMoniker = string.Join(", ", propertyNames.Take(5));

                if (table.Ignore)
                    console.WriteLine($"    table {tableName} [Ignored]");
                else
                {
                    console.Write($"    {tableName}", ConsoleColor.Blue);
                    console.Write(" -> ");
                    console.WriteLine(className, ConsoleColor.Green);
                }

                if (propertyNames.Length > 5)
                    propertyMoniker += $" [+{propertyNames.Length - 5}]";

                console.WriteLine($"        -> property {propertyMoniker}");
            }
        }
    }
}
