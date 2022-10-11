using System;

namespace Jerrycurl.Cqs.Metadata
{
    [Flags]
    public enum TableMetadataFlags
    {
        None = 0,
        Table = 1,
        Column = 2,
    }
}
