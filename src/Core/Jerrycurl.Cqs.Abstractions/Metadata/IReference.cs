using System;

namespace Jerrycurl.Cqs.Metadata;

public interface IReference : IEquatable<IReference>
{
    public IReferenceMetadata Metadata { get; }
    public IReferenceMetadata List { get; }
    public IReference Other { get; }
    public ReferenceFlags Flags { get; }
    public IReferenceKey Key { get; }
    public int Priority { get; }
}
