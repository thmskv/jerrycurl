using System;
using System.Collections.Generic;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Mvc.Metadata
{
    public interface IProjectionMetadata : IMetadata
    {
        MetadataIdentity Identity { get; }
        Type Type { get; }
        ITableMetadata Table { get; }
        ITableMetadata Column { get; }
        IReferenceMetadata Reference { get; }
        IReadOnlyList<IProjectionMetadata> Properties { get; }
        ProjectionMetadataFlags Flags { get; }

        IProjectionMetadata Input { get; }
        IProjectionMetadata Output { get; }

        IProjectionMetadata Item { get; }
        IProjectionMetadata List { get; }
    }
}
