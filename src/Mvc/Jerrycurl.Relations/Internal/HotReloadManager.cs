using System;
using System.Collections.Generic;
using System.Text;
using Jerrycurl.Relations.Internal;
using Jerrycurl.Relations.Metadata;


#if NET6_0_OR_GREATER
using System.Reflection.Metadata;

[assembly: MetadataUpdateHandler(typeof(HotReloadManager))]
#endif

namespace Jerrycurl.Relations.Internal;

internal static class HotReloadManager
{
    public static void ClearCache(Type[]? types)
    {
        SchemaStore.
        Console.WriteLine("ClearCache");
    }

    public static void UpdateApplication(Type[]? types)
    {
        // Re-render the list of properties
        Console.WriteLine("UpdateApplication");
    }
}