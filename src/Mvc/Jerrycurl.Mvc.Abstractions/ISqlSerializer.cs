using System.Collections.Generic;

namespace Jerrycurl.Mvc;

public interface ISqlSerializer<TData>
{
    public IEnumerable<TData> Serialize(ISqlOptions options);
}
