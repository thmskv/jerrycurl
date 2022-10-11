using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Jerrycurl.Cqs.Metadata;

namespace Jerrycurl.Cqs.Queries.Internal.IO.Readers
{
    internal class KeyReader
    {
        public IList<DataReader> Values { get; set; }
        public IReference Reference { get; set; }
        public IBindingMetadata Target { get; set; }
        public ParameterExpression Variable { get; set; }
        public Type KeyType { get; set; }
    }
}
