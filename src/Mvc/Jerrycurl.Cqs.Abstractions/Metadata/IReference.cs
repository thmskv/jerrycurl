using System;

namespace Jerrycurl.Cqs.Metadata
{
    public interface IReference : IEquatable<IReference>
    {
        IReferenceMetadata Metadata { get; }
        IReferenceMetadata List { get; }
        IReference Other { get; }
        ReferenceFlags Flags { get; }
        IReferenceKey Key { get; }
        int Priority { get; }
    }
}
