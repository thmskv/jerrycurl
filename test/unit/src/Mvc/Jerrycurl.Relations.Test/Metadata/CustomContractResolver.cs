using Jerrycurl.Reflection;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Relations.Test.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Relations.Test.Metadata
{
    public class CustomContractResolver : IRelationContractResolver
    {
        public IEnumerable<Attribute> GetAnnotations(IRelationMetadata metadata)
        {
            if (metadata.Parent != null && metadata.Parent.Type.IsOpenGeneric(typeof(CustomList<>), out Type _))
                yield return new CustomAttribute();
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
    }
}
