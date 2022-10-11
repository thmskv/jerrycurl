using System.Collections.Generic;
using System.Data.Common;

namespace Jerrycurl.Relations
{
    public interface IRelation
    {
        IRelationHeader Header { get; }
        IField Source { get; }
        IRelationReader GetReader();
        DbDataReader GetDataReader(IEnumerable<string> header);
        DbDataReader GetDataReader();

        IEnumerable<ITuple> Body { get; }
    }
}
