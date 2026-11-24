using System;
using System.Collections.Generic;
using System.Reflection;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata;

public interface IBindingMetadata : IMetadata
{
    public MetadataIdentity Identity { get; }
    public Type Type { get; }
    public MemberInfo Member { get; }
    public BindingMetadataFlags Flags { get; }
    public IBindingMetadata Parent { get; }
    public IBindingMetadata Item { get; }
    public IBindingMetadata Owner { get; }
    public IReadOnlyList<IBindingMetadata> Properties { get; }

    public IBindingParameterContract Parameter { get; }
    public IBindingCompositionContract Composition { get; }
    public IBindingValueContract Value { get; }
    public IBindingHelperContract Helper { get; }
}
