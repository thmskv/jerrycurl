using System;
using Jerrycurl.Diagnostics;

namespace Jerrycurl.Mvc.Internal;

internal class ProcCacheKey : IEquatable<ProcCacheKey>
{
    public Type PageType { get; }
    public Type ModelType { get; }
    public Type ResultType { get; }

    public ProcCacheKey(PageDescriptor descriptor, ProcArgs args)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(args);

        this.PageType = descriptor.PageType;
        this.ModelType = args.ModelType;
        this.ResultType = args.ResultType;
    }

    public bool Equals(ProcCacheKey other) => Equality.Combine(this, other, m => m.PageType, m => m.ModelType, m => m.ResultType);
    public override int GetHashCode() => HashCode.Combine(this.PageType, this.ModelType, this.ResultType);
    public override bool Equals(object obj) => (obj is ProcCacheKey other && this.Equals(other));
}
