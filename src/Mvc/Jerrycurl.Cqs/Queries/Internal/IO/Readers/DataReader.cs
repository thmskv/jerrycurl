using System.Linq.Expressions;
using Jerrycurl.Cqs.Queries.Internal.Parsing;

namespace Jerrycurl.Cqs.Queries.Internal.IO.Readers
{
    internal abstract class DataReader : BaseReader
    {
        public DataReader(Node node)
            : base(node)
        {

        }

        public bool CanBeDbNull { get; set; }
        public ParameterExpression IsDbNull { get; set; }
        public ParameterExpression Variable { get; set; }
    }
}
