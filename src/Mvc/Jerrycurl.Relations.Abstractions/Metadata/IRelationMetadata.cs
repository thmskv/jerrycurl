using System;
using System.Collections.Generic;
using System.Reflection;

namespace Jerrycurl.Relations.Metadata;

public interface IRelationMetadata : IMetadata
{
    public MetadataIdentity Identity { get; }
    public ISchema Schema { get; }
    public DotNotation Notation { get; }
    public IReadOnlyList<IRelationMetadata> Properties { get; }
    public IRelationMetadata Parent { get; }
    public IRelationMetadata Owner { get; }
    public IRelationMetadata Item { get; }
    public RelationMetadataFlags Flags { get; }
    public IReadOnlyList<Attribute> Annotations { get; }
    public IRelationMetadata Recursor { get; }
    public int Depth { get; set; }

    public MethodInfo WriteIndex { get; }
    public MethodInfo ReadIndex { get; }
    public MemberInfo Member { get; }
    public Type Type { get; }
}
