using System.Linq.Expressions;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Queries.Internal.Parsing;

namespace Jerrycurl.Cqs.Queries.Internal.IO.Readers
{
    internal class ColumnReader : DataReader
    {
        public ColumnReader(Node node)
            : base(node)
        {

        }

        public ColumnMetadata Column { get; set; }
        public ParameterExpression Helper { get; set; }
    }
}
