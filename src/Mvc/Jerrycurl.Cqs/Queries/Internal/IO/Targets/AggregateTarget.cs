using System.Linq.Expressions;
using System.Reflection;

namespace Jerrycurl.Cqs.Queries.Internal.IO.Targets
{
    internal class AggregateTarget 
    {
        public MethodInfo AddMethod { get; set; }
        public NewExpression NewList { get; set; }
    }
}
