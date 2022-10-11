using System.Collections.Generic;
using Jerrycurl.Cqs.Queries.Internal.IO.Readers;
using Jerrycurl.Cqs.Queries.Internal.IO.Targets;

namespace Jerrycurl.Cqs.Queries.Internal.IO.Writers
{
    internal class TargetWriter
    {
        public KeyReader PrimaryKey { get; set; }
        public List<JoinTarget> ForeignJoins { get; } = new List<JoinTarget>();
        public ListTarget List { get; set; }
        public JoinTarget Join { get; set; }
        public BaseReader Source { get; set; }
    }
}
