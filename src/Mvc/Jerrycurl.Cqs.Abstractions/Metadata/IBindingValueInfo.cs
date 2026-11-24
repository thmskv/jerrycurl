using System;
using System.Linq.Expressions;

namespace Jerrycurl.Cqs.Metadata;

public interface IBindingValueInfo
{
    public bool CanBeNull { get; }
    public bool CanBeDbNull { get; }
    public IBindingMetadata Metadata { get; }
    public Expression Value { get; }
    public Expression Helper { get; }
    public Type SourceType { get; }
    public Type TargetType { get; }
}
