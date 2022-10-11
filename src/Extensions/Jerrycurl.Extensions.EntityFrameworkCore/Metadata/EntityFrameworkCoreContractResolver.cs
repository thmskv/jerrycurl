using Jerrycurl.Collections;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Metadata.Annotations;
using Jerrycurl.Relations.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Extensions.EntityFrameworkCore.Metadata
{
    public class EntityFrameworkCoreContractResolver : IRelationContractResolver, ITableContractResolver
    {
        public int Priority { get; } = 1000;

        private IEntityType[] entities;

        public EntityFrameworkCoreContractResolver(DbContext dbContext)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));

            this.InitializeEntities(dbContext);
        }

        private void InitializeEntities(DbContext dbContext)
        {
            this.entities = dbContext.Model.GetEntityTypes().ToArray();
        }

        public IRelationContract GetContract(IRelationMetadata metadata) => null;

        public IEnumerable<Attribute> GetAnnotations(IRelationMetadata metadata)
        {
            IProperty property = this.FindProperty(metadata);
            IAnnotation[] propertyAnnotations = property?.GetAnnotations().ToArray() ?? new IAnnotation[0];

            IKey primaryKey = this.GetPrimaryKey(property);
            IForeignKey[] foreignKeys = property?.GetContainingForeignKeys().ToArray() ?? new IForeignKey[0];

            if (primaryKey != null)
            {
                string keyName = this.GetKeyName(primaryKey);
                int index = this.GetKeyIndex(primaryKey, property);

                yield return new KeyAttribute(keyName, index);
            }

            foreach (IForeignKey foreignKey in foreignKeys)
            {
                string primaryName = this.GetPrimaryKeyName(foreignKey);
                string foreignName = this.GetKeyName(foreignKey);
                int index = this.GetKeyIndex(foreignKey, property);

                yield return new RefAttribute(primaryName, index, foreignName);
            }

            if (propertyAnnotations.Any(a => a.Name == "SqlServer:ValueGenerationStrategy" && a.Value?.ToString() == "IdentityColumn"))
                yield return new IdAttribute();
        }

        public string[] GetTableName(ITableMetadata metadata)
        {
            IEntityType entity = this.entities.FirstOrDefault(e => e.ClrType.IsAssignableFrom(metadata.Relation.Type));

            string schemaName = this.GetSchemaName(entity);
            string tableName = this.GetTableName(entity);

            if (tableName == null)
                return null;

            return new[] { schemaName, tableName }.NotNull().ToArray();
        }

        public string GetColumnName(ITableMetadata metadata)
        {
            IProperty property = this.FindProperty(metadata.Relation);

            if (property != null && metadata.Relation.Member != null && metadata.Relation.Member.DeclaringType.IsAssignableFrom(property.DeclaringType.ClrType))
                return this.GetColumnName(property);

            return null;
        }


        private string GetPrimaryKeyName(IForeignKey key) => this.GetKeyName(key.PrincipalKey);
        private int GetKeyIndex(IKey key, IProperty property) => key.Properties.ToList().IndexOf(property);
        private int GetKeyIndex(IForeignKey key, IProperty property) => key.Properties.ToList().IndexOf(property);
        private IProperty FindProperty(IRelationMetadata metadata)
        {
            IEntityType parentEntity = this.entities.FirstOrDefault(e => metadata.Parent != null && e.ClrType.IsAssignableFrom(metadata.Parent.Type));

            return parentEntity?.GetProperties().FirstOrDefault(p => p.Name == metadata.Member?.Name);
        }

#if NET20_BASE
        private IKey GetPrimaryKey(IProperty property) => property?.GetContainingPrimaryKey();
        private string GetTableName(IEntityType entity) => entity?.Relational()?.TableName ?? entity?.ClrType.Name;
        private string GetSchemaName(IEntityType entity) => entity?.Relational()?.Schema;
        private string GetColumnName(IProperty property) => property?.Relational()?.ColumnName ?? property?.Name;
        private string GetKeyName(IKey key) => key?.Relational()?.Name;
        private string GetKeyName(IForeignKey key) => key?.Relational()?.Name;

#elif NET21_BASE
        private IKey GetPrimaryKey(IProperty property) => property?.FindContainingPrimaryKey();
        private string GetTableName(IEntityType entity) => entity?.GetTableName() ?? entity?.GetDefaultTableName();
        private string GetSchemaName(IEntityType entity) => entity?.GetSchema() ?? entity?.GetDefaultSchema();
        private string GetColumnName(IProperty property) => property?.GetColumnName() ?? property?.GetDefaultColumnName();
        private string GetKeyName(IKey key) => key?.GetName();
        private string GetKeyName(IForeignKey key) => key.GetConstraintName();
#endif
    }
}
