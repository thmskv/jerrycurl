using System;

namespace Jerrycurl.Relations
{
    public interface IRelationReader : ITuple, IDisposable
    {
        IRelation Relation { get; }
        bool Read();

        void CopyTo(IField[] target, int sourceIndex, int targetIndex, int length);
        void CopyTo(IField[] target, int length);

    }
}
