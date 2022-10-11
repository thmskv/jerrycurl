using System;

namespace Jerrycurl.Cqs.Metadata
{
    [Flags]
    public enum ReferenceKeyFlags
    {
        None = 0,
        Candidate = 1,
        Primary = Candidate | 2,
        Foreign = 4,
    }
}