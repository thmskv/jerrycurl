using System.Collections.Generic;
using Jerrycurl.Cqs.Queries.Internal.Parsing;

namespace Jerrycurl.Cqs.Queries.Internal.IO.Readers
{
    internal class DynamicReader : BaseReader
    {
        public DynamicReader(Node node)
        {
            this.Metadata = node.Metadata;
            this.Identity = node.Identity;
        }

        public IList<BaseReader> Properties { get; set; } = new List<BaseReader>();
    }
}
