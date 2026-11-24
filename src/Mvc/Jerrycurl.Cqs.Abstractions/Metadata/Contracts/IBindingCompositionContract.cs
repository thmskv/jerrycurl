using System.Linq.Expressions;
using System.Reflection;

namespace Jerrycurl.Cqs.Metadata;

public interface IBindingCompositionContract
{
    public MethodInfo Add { get; }
    public NewExpression Construct { get; }
    public MethodInfo AddDynamic { get; }
}
