using System.Collections.Generic;

namespace Jerrycurl.Relations;

public interface ITuple : IReadOnlyList<IField>
{
    public int Degree { get; }
}
