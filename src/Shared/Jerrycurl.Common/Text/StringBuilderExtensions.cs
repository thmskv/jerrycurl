using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Text
{
    internal static class StringBuilderExtensions
    {
#if NET20_BASE
        public static StringBuilder AppendJoin<T>(this StringBuilder builder, string separator, IEnumerable<T> values)
            => builder.Append(string.Join(separator, values));
#endif
    }
}
