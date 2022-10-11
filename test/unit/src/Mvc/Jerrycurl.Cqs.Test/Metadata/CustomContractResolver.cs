using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Test.Models;
using Jerrycurl.Reflection;
using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Jerrycurl.Cqs.Test.Metadata
{
    public class CustomContractResolver : IBindingContractResolver, IRelationContractResolver
    {
        public int Priority => 10;

        public IEnumerable<Attribute> GetAnnotations(IRelationMetadata metadata) => null;

        public IBindingCompositionContract GetCompositionContract(IBindingMetadata metadata)
        {
            if (metadata.Type.IsOpenGeneric(typeof(CustomList<>), out Type itemType))
            {
                return new BindingCompositionContract()
                {
                    Construct = Expression.New(typeof(CustomList<>).MakeGenericType(itemType)),
                    Add = typeof(ICollection<>).MakeGenericType(itemType).GetMethod("Add"),
                };
            }

            return null;
        }

        public IRelationContract GetContract(IRelationMetadata metadata)
        {
            if (metadata.Type.IsOpenGeneric(typeof(CustomList<>), out Type itemType))
            {
                var indexer = metadata.Type.GetProperties().FirstOrDefault(pi => pi.Name == "Item" && pi.GetIndexParameters().FirstOrDefault()?.ParameterType == typeof(int));

                return new RelationContract()
                {
                    ItemType = itemType,
                    ReadIndex = indexer.GetMethod,
                    WriteIndex = indexer.SetMethod,
                };
            }

            return null;
        }

        public IBindingHelperContract GetHelperContract(IBindingMetadata metadata) => null;
        public IBindingParameterContract GetParameterContract(IBindingMetadata metadata) => null;
        public IBindingValueContract GetValueContract(IBindingMetadata metadata)
        {
            if (metadata.Type.IsOpenGeneric(typeof(CustomList<>), out Type itemType))
            {
                return new BindingValueContract()
                {
                    Read = null,
                    Convert = info =>
                    {
                        var listType = typeof(CustomList<>).MakeGenericType(itemType);
                        var collectionType = typeof(ICollection<>).MakeGenericType(itemType);

                        var newList = Expression.New(listType);
                        var addMethod = collectionType.GetMethod("Add");
                        var variable = Expression.Variable(listType);

                        var assignList = Expression.Assign(variable, newList);
                        var addDefault = Expression.Call(variable, addMethod, Expression.Default(itemType));

                        return Expression.Block(new[] { variable }, assignList, addDefault, variable);
                    }
                };
            }

            return null;
        }
    }
}
