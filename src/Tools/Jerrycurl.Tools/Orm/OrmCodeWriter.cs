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
        public async Task WriteAsync(SchemaModel schema, string writePath)
        {
            PathHelper.EnsureDirectory(writePath);

            using (StreamWriter fileWriter = new StreamWriter(writePath, append: false, Encoding.UTF8))
            {
                CSharpWriter writer = new CSharpWriter(fileWriter);

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
            }
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
    }
}
