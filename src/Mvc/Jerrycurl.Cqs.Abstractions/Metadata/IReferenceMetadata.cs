using System;
using System.Collections.Generic;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata
{
    public interface IReferenceMetadata : IMetadata
    {
        MetadataIdentity Identity { get; }
        Type Type { get; }
        IReadOnlyList<IReference> References { get; }
        IReadOnlyList<IReferenceKey> Keys { get; }
        ReferenceMetadataFlags Flags { get; }
        IReadOnlyList<IReferenceMetadata> Properties { get; }
        IReferenceMetadata Item { get; }
        IReadOnlyList<Attribute> Annotations { get; }
    }
}
