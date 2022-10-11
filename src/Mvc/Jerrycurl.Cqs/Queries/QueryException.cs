using System;
using System.Runtime.Serialization;

namespace Jerrycurl.Cqs.Queries
{
    [Serializable]
    public class QueryException : Exception
    {
        public QueryException()
        {

        }

        public QueryException(string message)
            : base(message)
        {

        }

        public QueryException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected QueryException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        internal static QueryException InvalidQueryType(QueryType queryType)
            => new QueryException($"Invalid query type '{queryType}': Only List and Aggregate types are supported.");
    }
}
