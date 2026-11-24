using System.Collections.Generic;
using System.Data.Common;

namespace Jerrycurl.Relations;

public interface IRelation
{
    public IRelationHeader Header { get; }
    public IField Source { get; }
    public IRelationReader GetReader();
    public DbDataReader GetDataReader(IEnumerable<string> header);
    public DbDataReader GetDataReader();

    public IEnumerable<ITuple> Body { get; }
}
