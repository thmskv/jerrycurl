using Jerrycurl.Collections;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Metadata.Annotations;
using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Jerrycurl.Mvc.Metadata
{
    internal class JsonMetadataBuilder : Collection<IJsonContractResolver>, IMetadataBuilder<IJsonMetadata>
    {
        public IJsonMetadata GetMetadata(IMetadataBuilderContext context) => this.GetMetadata(context, context.Relation);

        private IJsonMetadata GetMetadata(IMetadataBuilderContext context, IRelationMetadata relation)
        {
            IJsonMetadata parent = context.GetMetadata<IJsonMetadata>(relation.Parent.Identity.Name) ?? this.GetMetadata(context, relation.Parent);

            if (parent == null || relation == null)
                return null;

            JsonMetadata metadata = new JsonMetadata(relation);

            if (this.HasJsonAttribute(relation) || relation.HasFlag(RelationMetadataFlags.Item))
            {
                metadata.MemberOf = metadata;
                metadata.IsRoot = true;
                metadata.Path = "$";
            }
            else
            {
                metadata.MemberOf = parent.MemberOf;
                metadata.IsRoot = false;
                metadata.Path = $"{parent.Path}.{this.GetPropertyNameFromContract(metadata)}";
            }

            context.AddMetadata<IJsonMetadata>(metadata);

            return metadata;
        }

        public void Initialize(IMetadataBuilderContext context)
        {
            JsonMetadata metadata = new JsonMetadata(context.Relation);

            metadata.MemberOf = metadata;
            metadata.Path = "$";
            metadata.IsRoot = true;

            context.AddMetadata<IJsonMetadata>(metadata);
        }

        private bool HasJsonAttribute(IRelationMetadata relation) => relation.Annotations.OfType<JsonAttribute>().Any();
        private string GetPropertyNameFromContract(IJsonMetadata metadata)
        {
            string propertyName = this.Reverse().NotNull(c => c.GetPropertyName(metadata)).FirstOrDefault();

            return propertyName ?? metadata.Relation.Member?.Name ?? throw new InvalidOperationException("No member found.");
        }
    }
}
