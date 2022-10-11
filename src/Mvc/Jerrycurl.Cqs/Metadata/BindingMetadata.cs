using System;
using System.Collections.Generic;
using System.Reflection;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata
{
    internal class BindingMetadata : IBindingMetadata
    {
        public IRelationMetadata Relation { get; }
        public MetadataIdentity Identity => this.Relation.Identity;
        public Type Type => this.Relation.Type;
        public MemberInfo Member => this.Relation.Member;

        public BindingMetadataFlags Flags { get; set; }
        public BindingMetadata Parent { get; set; }
        public Lazy<IReadOnlyList<BindingMetadata>> Properties { get; set; }
        public IReadOnlyList<Attribute> CustomAttributes { get; set; }
        public BindingMetadata Item { get; set; }
        public BindingMetadata MemberOf { get; set; }

        public IBindingParameterContract Parameter { get; set; }
        public IBindingCompositionContract Composition { get; set; }
        public IBindingValueContract Value { get; set; }
        public IBindingHelperContract Helper { get; set; }

        IBindingMetadata IBindingMetadata.Parent => this.Parent;
        IReadOnlyList<IBindingMetadata> IBindingMetadata.Properties => this.Properties.Value;
        IBindingMetadata IBindingMetadata.Item => this.Item;
        IBindingMetadata IBindingMetadata.Owner => this.MemberOf;

        public BindingMetadata(IRelationMetadata relation)
        {
            this.Relation = relation ?? throw new ArgumentNullException(nameof(relation));
        }

        public override string ToString() => $"IBindingMetadata: {this.Identity}";
    }
}
