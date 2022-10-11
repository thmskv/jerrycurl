using System;
using System.Collections.Generic;
using System.Reflection;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata
{
    public interface IBindingMetadata : IMetadata
    {
        MetadataIdentity Identity { get; }
        Type Type { get; }
        MemberInfo Member { get; }
        BindingMetadataFlags Flags { get; }
        IBindingMetadata Parent { get; }
        IBindingMetadata Item { get; }
        IBindingMetadata Owner { get; }
        IReadOnlyList<IBindingMetadata> Properties { get; }

        IBindingParameterContract Parameter { get; }
        IBindingCompositionContract Composition { get; }
        IBindingValueContract Value { get; }
        IBindingHelperContract Helper { get; }
    }
}
