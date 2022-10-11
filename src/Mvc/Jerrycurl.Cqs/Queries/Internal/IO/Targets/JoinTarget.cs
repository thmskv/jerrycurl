using System.Linq.Expressions;
using System.Reflection;
using Jerrycurl.Cqs.Queries.Internal.IO.Readers;

namespace Jerrycurl.Cqs.Queries.Internal.IO.Targets
{
    internal class JoinTarget 
    {
        public ListTarget List { get; set; }
        public MethodInfo AddMethod { get; set; }
        public NewExpression NewList { get; set; }
        public KeyReader Key { get; set; }
        public ParameterExpression Buffer { get; set; }
        public int Index { get; set; }
    }
}
