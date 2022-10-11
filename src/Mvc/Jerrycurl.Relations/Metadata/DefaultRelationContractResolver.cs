using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jerrycurl.Reflection;

namespace Jerrycurl.Relations.Metadata
{
    public class DefaultRelationContractResolver : IRelationContractResolver
    {
        public IRelationContract GetContract(IRelationMetadata metadata)
        {
            if (this.IsEnumerable(metadata))
            {
                return new RelationContract()
                {
                    ItemType = this.GetGenericItemType(metadata),
                    ReadIndex = this.GetListIndexReader(metadata),
                    WriteIndex = this.GetListIndexWriter(metadata),
                };
            }
            else if (this.IsOneDimensionalArray(metadata))
            {
                return new RelationContract()
                {
                    ItemType = this.GetArrayItemType(metadata),
                    ReadIndex = this.GetArrayIndexReader(metadata),
                    WriteIndex = this.GetArrayIndexWriter(metadata),
                };
            }
            else if (this.IsOneType(metadata))
            {
                return new RelationContract()
                {
                    ItemType = this.GetGenericItemType(metadata),
                };
            }

            return null;
        }

        public IEnumerable<Attribute> GetAnnotations(IRelationMetadata metadata)
        {
            return metadata.Type.GetCustomAttributes(inherit: true).OfType<Attribute>().Concat(metadata.Member?.GetCustomAttributes() ?? Array.Empty<Attribute>());
        }

        private bool IsEnumerable(IRelationMetadata metadata)
        {
            if (!metadata.Type.IsGenericType)
                return false;

            Type[] allowedTypes = new Type[]
            {
                typeof(IList<>),
                typeof(List<>),
                typeof(IEnumerable<>),
                typeof(ICollection<>),
                typeof(IReadOnlyList<>),
                typeof(IReadOnlyCollection<>),
            };

            Type openType = metadata.Type.GetGenericTypeDefinition();

            if (!allowedTypes.Contains(openType))
                return false;

            return true;
        }

        private bool IsOneType(IRelationMetadata metadata)
        {
            if (!metadata.Type.IsGenericType)
                return false;

            Type openType = metadata.Type.GetGenericTypeDefinition();

            if (openType == typeof(One<>))
                return true;

            return false;
        }

        private bool IsOneDimensionalArray(IRelationMetadata metadata) => (metadata.Type.IsArray && metadata.Type.GetArrayRank() == 1);

        private Type GetGenericItemType(IRelationMetadata metadata) => metadata.Type.GetGenericArguments()[0];

        private MethodInfo GetListIndexWriter(IRelationMetadata metadata) => this.GetListIndexer(metadata)?.SetMethod;
        private MethodInfo GetListIndexReader(IRelationMetadata metadata) => this.GetListIndexer(metadata)?.GetMethod;

        private Type GetArrayItemType(IRelationMetadata metadata) => metadata.Type.GetElementType();
        private MethodInfo GetArrayIndexWriter(IRelationMetadata metadata) => metadata.Type.GetMethod("Set", new[] { typeof(int), metadata.Type.GetElementType() });
        private MethodInfo GetArrayIndexReader(IRelationMetadata metadata) => metadata.Type.GetMethod("Get", new[] { typeof(int) });

        private PropertyInfo GetListIndexer(IRelationMetadata metadata)
        {
            Type[] allowedTypes = new[]
            {
                typeof(IList<>),
                typeof(List<>),
                typeof(IEnumerable<>),
                typeof(ICollection<>),
                typeof(IReadOnlyList<>),
                typeof(IReadOnlyCollection<>),
            };
            Type[] convertTypes = new[]
            {
                typeof(IEnumerable<>),
                typeof(ICollection<>),
                typeof(IReadOnlyCollection<>),
            };

            Type openType = metadata.Type.GetGenericTypeDefinition();

            if (!allowedTypes.Contains(openType))
                return null;

            if (convertTypes.Contains(openType))
            {
                Type itemType = metadata.Type.GetGenericArguments()[0];
                Type listType = typeof(IList<>).MakeGenericType(itemType);

                return listType.GetIndexer();
            }

            return metadata.Type.GetIndexer();
        }
    }
}
