using System;
using System.Collections.Generic;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Razor.Metadata;

public interface IProjectionMetadata : IMetadata
{
    public MetadataIdentity Identity { get; }
    public Type Type { get; }
    public ITableMetadata Table { get; }
    public ITableMetadata Column { get; }
    public IReferenceMetadata Reference { get; }
    public IReadOnlyList<IProjectionMetadata> Properties { get; }
    public ProjectionMetadataFlags Flags { get; }

    public IProjectionMetadata Input { get; }
    public IProjectionMetadata Output { get; }

    public IProjectionMetadata Item { get; }
    public IProjectionMetadata List { get; }
}
