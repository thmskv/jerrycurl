using System;
using System.Collections;
using System.Collections.Generic;

namespace Jerrycurl.Relations.Metadata;

public sealed class ConventionList<T> : IReadOnlyList<T>
    where T : class
{
    private readonly List<T> items = [];

    public ConventionList()
    {

    }

    public ConventionList(IEnumerable<T> conventions)
    {
        ArgumentNullException.ThrowIfNull(conventions);

        foreach (T convention in conventions)
            this.Add(convention);
    }

    public int Count => this.items.Count;
    public T this[int index] => this.items[index];

    public void Add(T convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        this.items.Add(convention);
    }

    public bool Remove(T convention) => this.items.Remove(convention);
    public int Remove<TConvention>() where TConvention : T => this.items.RemoveAll(c => c is TConvention);
    public void Clear() => this.items.Clear();

    public List<T>.Enumerator GetEnumerator() => this.items.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => this.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
