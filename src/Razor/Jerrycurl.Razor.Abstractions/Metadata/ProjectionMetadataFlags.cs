using System;

namespace Jerrycurl.Razor.Metadata;

[Flags]
public enum ProjectionMetadataFlags
{
    None = 0,
    Input = 1,
    Output = 2,
    Identity = 4,
    Cascade = 8,
}
