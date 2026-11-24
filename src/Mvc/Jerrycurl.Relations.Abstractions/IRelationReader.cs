using System;

namespace Jerrycurl.Relations;

public interface IRelationReader : ITuple, IDisposable
{
    public IRelation Relation { get; }
    public bool Read();

    public void CopyTo(IField[] target, int sourceIndex, int targetIndex, int length);
    public void CopyTo(IField[] target, int length);

}
