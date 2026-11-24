using System.Collections.Generic;

namespace Jerrycurl.Razor;

public interface ISqlSerializer<TData>
{
    public IEnumerable<TData> Serialize(ISqlOptions options);
}
