using System;
using System.Collections.Generic;
using System.Reflection;

namespace Jerrycurl.Relations.Metadata
{
    public interface IRelationMetadata : IMetadata
    {
        MetadataIdentity Identity { get; }
        ISchema Schema { get; }
        DotNotation Notation { get; }
        IReadOnlyList<IRelationMetadata> Properties { get; }
        IRelationMetadata Parent { get; }
        IRelationMetadata Owner { get; }
        IRelationMetadata Item { get; }
        RelationMetadataFlags Flags { get; }
        IReadOnlyList<Attribute> Annotations { get; }
        IRelationMetadata Recursor { get; }
        int Depth { get; set; }

        MethodInfo WriteIndex { get; }
        MethodInfo ReadIndex { get; }
        MemberInfo Member { get; }
        Type Type { get; }
    }
}
