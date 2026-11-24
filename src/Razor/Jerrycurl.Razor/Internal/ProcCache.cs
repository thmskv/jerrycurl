using System;
using System.Collections.Concurrent;

namespace Jerrycurl.Razor.Internal;

internal static class ProcCache
{
    public static ConcurrentDictionary<ProcCacheKey, ProcFactory> Procs { get; } = new ConcurrentDictionary<ProcCacheKey, ProcFactory>();
    public static ConcurrentDictionary<Type, PageFactory> Pages { get; } = new ConcurrentDictionary<Type, PageFactory>();
    public static ConcurrentDictionary<Type, Lazy<IDomainOptions>> Domains { get; } = new ConcurrentDictionary<Type, Lazy<IDomainOptions>>();

    public static ConcurrentDictionary<Type, PageDescriptor> Descriptors { get; } = new ConcurrentDictionary<Type, PageDescriptor>();
}
