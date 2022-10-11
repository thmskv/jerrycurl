using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Jerrycurl.Collections;
using Jerrycurl.Reflection;

namespace Jerrycurl.Relations.Metadata
{
    public class RelationMetadataBuilder : Collection<IRelationContractResolver>, IMetadataBuilder<IRelationMetadata>
    {
        public IRelationContractResolver DefaultResolver { get; set; } = new DefaultRelationContractResolver();

        public void Initialize(IMetadataBuilderContext context) { }
        public IRelationMetadata GetMetadata(IMetadataBuilderContext context) => context.Relation;

        internal RelationMetadataBuilder()
        {
            
        }

        internal IRelationMetadata GetMetadata(Schema schema, MetadataIdentity identity)
        {
            MetadataIdentity parentIdentity = identity.Pop();
            IRelationMetadata parent = schema.GetCachedMetadata<IRelationMetadata>(parentIdentity.Name) ?? this.GetMetadata(schema, parentIdentity);

            if (parent == null)
                return null;
            else if (parent.Item != null && parent.Item.Identity.Equals(identity))
                return parent.Item;

            return parent.Properties.FirstOrDefault(m => m.Identity.Equals(identity));
        }

        internal void Initialize(Schema schema, Type modelType)
        {
            MetadataIdentity identity = new MetadataIdentity(schema);
            RelationMetadata metadata = new RelationMetadata(schema, identity)
            {
                Flags = RelationMetadataFlags.Model | RelationMetadataFlags.Readable,
                Type = modelType,
            };

            metadata.Owner = metadata;
            metadata.Properties = this.CreateLazy(() => this.CreateProperties(metadata));
            metadata.Depth = 0;

            metadata.Annotations = this.CreateAnnotations(metadata).ToList();
            metadata.Item = this.CreateItem(metadata);

            if (metadata.Item != null)
                metadata.Flags |= RelationMetadataFlags.List;

            schema.AddMetadata<IRelationMetadata>(metadata);
        }

        private IRelationContract GetContract(RelationMetadata metadata)
        {
            IEnumerable<IRelationContractResolver> allResolvers = new[] { this.DefaultResolver }.Concat(this);

            IRelationContract contract = allResolvers.Reverse().NotNull(cr => cr.GetContract(metadata)).FirstOrDefault();

            if (contract != null)
                this.ValidateContract(metadata, contract);

            return contract;
        }

        private void ValidateContract(RelationMetadata metadata, IRelationContract contract)
        {
            if (contract.ItemType == null)
                throw MetadataBuilderException.InvalidContract(metadata, "Item type cannot be null.");
            else if (string.IsNullOrWhiteSpace(contract.ItemName))
                throw MetadataBuilderException.InvalidContract(metadata, "Item name cannot be empty.");
            else
            {
                Type enumerableType = typeof(IEnumerable<>).MakeGenericType(contract.ItemType);

                if (!enumerableType.IsAssignableFrom(metadata.Type))
                    throw MetadataBuilderException.InvalidContract(metadata, $"List of type '{metadata.Type.GetSanitizedName()}' cannot be converted to '{enumerableType.GetSanitizedName()}'.");
            }

            if (contract.ReadIndex != null && !contract.ReadIndex.HasSignature(contract.ItemType, typeof(int)))
                throw MetadataBuilderException.InvalidContract(metadata, $"ReadIndex method must have signature '{contract.ItemType.GetSanitizedName()} (int)'.");

            if (contract.WriteIndex != null && !contract.WriteIndex.HasSignature(typeof(void), typeof(int), contract.ItemType))
                throw MetadataBuilderException.InvalidContract(metadata, $"WriteIndex method must have signature 'void (int, {contract.ItemType.GetSanitizedName()})'.");
        }

        private Lazy<IReadOnlyList<TItem>> CreateLazy<TItem>(Func<IEnumerable<TItem>> factory) => new Lazy<IReadOnlyList<TItem>>(() => factory().ToList());

        private IEnumerable<Attribute> CreateAnnotations(RelationMetadata metadata)
        {
            IEnumerable<IRelationContractResolver> allResolvers = new[] { this.DefaultResolver }.Concat(this);

            return allResolvers.NotNull().SelectMany(cr => cr.GetAnnotations(metadata) ?? Array.Empty<Attribute>()).NotNull();
        }

        private IEnumerable<RelationMetadata> CreateProperties(RelationMetadata parent)
        {
            IEnumerable<MemberInfo> members = parent.Type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (MemberInfo member in members.Where(m => this.IsFieldOrNonIndexedProperty(m)))
            {
                RelationMetadata property = this.CreateProperty(parent, member);

                if (property != null)
                    yield return property;
            }
        }

        private RelationMetadata CreateProperty(RelationMetadata parent, MemberInfo memberInfo)
        {
            MetadataIdentity propertyId = parent.Identity.Push(memberInfo.Name);

            RelationMetadata metadata = new RelationMetadata(parent.Schema, propertyId)
            {
                Type = this.GetMemberType(memberInfo),
                Parent = parent,
                Member = memberInfo,
                Owner = parent.Owner,
                Flags = RelationMetadataFlags.Property,
                Depth = parent.Depth,
            };

            metadata.Item = this.CreateItem(metadata);
            metadata.Properties = this.CreateLazy(() => this.CreateProperties(metadata));
            metadata.Annotations = this.CreateAnnotations(metadata).ToList();

            if (metadata.Item != null)
                metadata.Flags |= RelationMetadataFlags.List;

            if (memberInfo is PropertyInfo pi)
            {
                if (pi.CanRead)
                    metadata.Flags |= RelationMetadataFlags.Readable;

                if (pi.CanWrite)
                    metadata.Flags |= RelationMetadataFlags.Writable;
            }
            else if (memberInfo is FieldInfo)
                metadata.Flags |= RelationMetadataFlags.Readable | RelationMetadataFlags.Writable;

            parent.Schema.AddMetadata<IRelationMetadata>(metadata);

            if (metadata.Item != null)
                parent.Schema.AddMetadata<IRelationMetadata>(metadata.Item);

            this.AddRecursors(metadata);

            return metadata;
        }

        private IRelationMetadata GetRecursiveParent(RelationMetadata metadata)
        {
            IRelationMetadata current = metadata.Parent;
            IRelationMetadata stop = current.Owner.Parent ?? current.Owner;

            while (current != stop)
            {
                if (current.Type == metadata.Type)
                    return current;

                current = current.Parent;
            }

            return null;
        }

        private void AddRecursors(RelationMetadata metadata)
        {
            metadata.Recursor = this.CreateRecursor(metadata);

            if (metadata.Recursor != null)
                metadata.Flags |= RelationMetadataFlags.Recursive | RelationMetadataFlags.List;

            if (metadata.Item != null)
            {
                metadata.Item.Recursor = this.CreateRecursor(metadata.Item);

                if (metadata.Item.Recursor != null)
                    metadata.Item.Flags |= RelationMetadataFlags.Recursive;
            }
        }

        private Lazy<IRelationMetadata> CreateRecursor(RelationMetadata metadata)
        {
            if (metadata.HasFlag(RelationMetadataFlags.Item))
            {
                IRelationMetadata recursiveParent = this.GetRecursiveParent(metadata);

                if (recursiveParent != null)
                {
                    string recursivePath = metadata.Notation.Path(recursiveParent.Identity.Name, metadata.Parent.Identity.Name);
                    string otherPath = metadata.Notation.Combine(metadata.Identity.Name, recursivePath);

                    MetadataIdentity otherId = metadata.Identity.Push(recursivePath);

                    return new Lazy<IRelationMetadata>(() => this.GetMetadata(metadata.Schema, otherId));
                }
            }
            else if (metadata.Owner.Recursor != null)
            {
                IRelationContract contract = this.GetContract(metadata);

                if (contract != null && metadata.Owner.Type.Equals(contract.ItemType))
                    return new Lazy<IRelationMetadata>(() => metadata.Owner);
            }

            return null;
        }

        private RelationMetadata CreateItem(RelationMetadata parent)
        {
            if (parent.Owner.HasFlag(RelationMetadataFlags.Recursive))
            {
                MemberInfo parentMember = parent.Owner.Parent?.Member;
                MemberInfo thisMember = parent.Member;

                if (parentMember != null && parentMember.Equals(thisMember))
                    return null;
            }

            IRelationContract contract = this.GetContract(parent);

            if (contract == null)
                return null;

            MetadataIdentity itemId = parent.Identity.Push(contract.ItemName ?? "Item");
            RelationMetadata metadata = new RelationMetadata(parent.Schema, itemId)
            {
                Parent = parent,
                Type = contract.ItemType,
                Flags = RelationMetadataFlags.Item,
                ReadIndex = contract.ReadIndex,
                WriteIndex = contract.WriteIndex,
                Depth = parent.Depth + 1,
            };

            metadata.Owner = metadata;
            metadata.Item = this.CreateItem(metadata);
            metadata.Properties = this.CreateLazy(() => this.CreateProperties(metadata));
            metadata.Annotations = this.CreateAnnotations(metadata).ToList();

            if (contract.ReadIndex != null)
                metadata.Flags |= RelationMetadataFlags.Readable;

            if (contract.WriteIndex != null)
                metadata.Flags |= RelationMetadataFlags.Writable;

            if (metadata.Item != null)
                metadata.Flags |= RelationMetadataFlags.List;

            return metadata;
        }

        private bool IsFieldOrNonIndexedProperty(MemberInfo memberInfo)
        {
            if (memberInfo is PropertyInfo pi)
                return (pi.GetIndexParameters().Length == 0 && pi.GetAccessors(nonPublic: true).Any(m => m.IsAssembly || m.IsPublic));
            else if (memberInfo is FieldInfo fi)
                return fi.IsPublic;

            return false;
        }

        private Type GetMemberType(MemberInfo member)
        {
            if (member.MemberType == MemberTypes.Property)
                return ((PropertyInfo)member).PropertyType;
            else if (member.MemberType == MemberTypes.Field)
                return ((FieldInfo)member).FieldType;

            return null;
        }
    }
}
