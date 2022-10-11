using System;

namespace Jerrycurl.Cqs.Metadata
{
    [Flags]
    public enum ReferenceFlags
    {
        None = 0,
        Parent = 1,
        Child = 2,
        Candidate = 4,
        Foreign = 8,
        Primary = Candidate | 16,
        One = 32,
        Many = 64,
        Self = 128,
    }
}
