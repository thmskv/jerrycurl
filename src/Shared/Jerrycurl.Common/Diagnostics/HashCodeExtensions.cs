#if NET
using System;
using System.Collections.Generic;

namespace Jerrycurl.Diagnostics;

internal static class HashCodeExtensions
{
    extension(HashCode)
    {
        public static int CombineAll<T>(IEnumerable<T> items)
        {
            HashCode hashCode = new HashCode();

            foreach (T value in items)
                hashCode.Add(value);

            return hashCode.ToHashCode();
        }
    }

    extension(ref HashCode hashCode)
    {
        public void AddRange<T>(IEnumerable<T> values)
        {
            foreach (T value in values)
                hashCode.Add(value);
        }

        public void AddRange<T>(IEnumerable<T> values, IEqualityComparer<T> comparer)
        {
            foreach (T value in values)
                hashCode.Add(value, comparer);
        }
    }
}
#endif
