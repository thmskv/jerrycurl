using System.Collections.Generic;

namespace Jerrycurl.Relations
{
    public interface ITuple : IReadOnlyList<IField>
    {
        int Degree { get; }
    }
}
