using System;

namespace Jerrycurl.Cqs.Metadata;

public interface IBindingHelperContract
{
    public object Object { get; }
    public Type Type { get; }
}
