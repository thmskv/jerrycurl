using System;
using System.Collections.Generic;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata;

public interface IReferenceMetadata : IMetadata
{
    public MetadataIdentity Identity { get; }
    public Type Type { get; }
    public IReadOnlyList<IReference> References { get; }
    public IReadOnlyList<IReferenceKey> Keys { get; }
    public ReferenceMetadataFlags Flags { get; }
    public IReadOnlyList<IReferenceMetadata> Properties { get; }
    public IReferenceMetadata Item { get; }
    public IReadOnlyList<Attribute> Annotations { get; }
}
