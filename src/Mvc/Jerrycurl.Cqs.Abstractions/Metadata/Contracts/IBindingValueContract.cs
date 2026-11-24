using System.Linq.Expressions;
using System.Reflection;

namespace Jerrycurl.Cqs.Metadata;

public delegate Expression BindingValueConverter(IBindingValueInfo valueInfo);
public delegate MethodInfo BindingValueReader(IBindingColumnInfo columnInfo);

public interface IBindingValueContract
{
    public BindingValueReader Read { get; }
    public BindingValueConverter Convert { get; }
}
