using System;
using System.Collections.Generic;

namespace Jerrycurl.Collections;

internal static class DictionaryExtensions
{
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, TValue value) => d.GetOrAdd(key, () => value);
    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key, Func<TValue> valueFactory)
    {
        if (d.TryGetValue(key, out TValue value2))
            return value2;

        TValue value;

        d.Add(key, value = valueFactory());

        return value;
    }

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> d, TKey key)
        where TValue : new()
        => d.GetOrAdd(key, new TValue());
}
