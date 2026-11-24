using System;
using System.Collections.Generic;

namespace Jerrycurl.Cqs.Metadata;

public interface IReferenceKey : IEquatable<IReferenceKey>
{
    public string Name { get; }
    public string Other { get; }
    public ReferenceKeyFlags Flags { get; }
    public IReadOnlyList<IReferenceMetadata> Properties { get; }
}
