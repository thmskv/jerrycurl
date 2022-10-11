using System;
using System.Linq;
using Jerrycurl.Collections;
using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Cqs.Metadata
{
    public class DefaultTableContractResolver : ITableContractResolver
    {
        public int Priority => 0;

        public string GetColumnName(ITableMetadata metadata)
        {
            TableAttribute table = metadata.Relation.Parent?.Annotations?.FirstOfType<TableAttribute>();
            ColumnAttribute column = metadata.Relation.Annotations?.FirstOfType<ColumnAttribute>();

            if (column != null && string.IsNullOrWhiteSpace(column.Name))
                return this.GetDefaultColumnName(metadata);
            else if (column != null)
                return column.Name;
            else if (table != null)
            {
                Type declaringType = this.GetDeclaringTypeOfAttribute(metadata.Relation.Parent.Type, table);

                if (declaringType == metadata.Relation.Member?.DeclaringType)
                    return this.GetDefaultColumnName(metadata);
            }

            return null;
        }

        private string GetDefaultColumnName(ITableMetadata metadata)
            => metadata.Relation.Member?.Name ?? metadata.Identity.Notation.Member(metadata.Identity.Name);

        public string[] GetTableName(ITableMetadata metadata)
        {
            TableAttribute table = metadata.Relation.Annotations?.FirstOfType<TableAttribute>();

            if (table != null)
            {
                string[] tableName = table.Parts?.Where(p => !string.IsNullOrWhiteSpace(p)).ToArray();

                if (tableName == null || tableName.Length == 0)
                {
                    Type declaringType = this.GetDeclaringTypeOfAttribute(metadata.Relation.Type, table);

                    return new[] { declaringType?.Name ?? metadata.Relation.Type.Name };
                }

                return tableName;
            }

            return null;
        }

        private Type GetDeclaringTypeOfAttribute(Type type, Attribute attribute)
        {
            while (type != null && type.BaseType != null && type.BaseType.GetCustomAttributes(inherit: false).Any(a => a.Equals(attribute)))
                type = type.BaseType;

            return type;
        }
    }
}
