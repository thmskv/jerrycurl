using System;
using System.Collections;
using System.Collections.Generic;
using Jerrycurl.Relations.Internal.Caching;
using Jerrycurl.Relations.Internal.Compilation;
using Jerrycurl.Relations.Internal.Queues;

namespace Jerrycurl.Relations
{
    public class RelationReader : IRelationReader
    {
        public IRelation Relation { get; }
        public int Degree => this.Relation.Header.Degree;

        int ITuple.Degree => this.Degree;
        int IReadOnlyCollection<IField>.Count => this.Degree;

        private int currentIndex;
        private Func<bool> readFactory;
        private RelationBuffer buffer;
        private bool hasCompleted;
        
        public RelationReader(IRelation relation)
        {
            this.Relation = relation ?? throw new ArgumentNullException(nameof(relation));
            this.readFactory = this.ReadFirst;
        }

        internal RelationBuffer Buffer
        {
            get
            {
                if (this.hasCompleted)
                    throw RelationException.NoDataAvailable(this.Relation.Header);
                else if (this.buffer == null)
                    throw RelationException.NoDataAvailableCallRead(this.Relation.Header);

                return this.buffer;
            }
        }
        public void CopyTo(IField[] target, int sourceIndex, int targetIndex, int length)
            => Array.Copy(this.Buffer.Fields, sourceIndex, target, targetIndex, length);


        public void CopyTo(IField[] target, int length)
            => Array.Copy(this.Buffer.Fields, target, length);

        public IField this[int index]
        {
            get
            {
                if (index < 0 || index >= this.Degree)
                    throw RelationException.IndexOutOfRange(this.Relation.Header, index);

                return this.Buffer.Fields[index];
            }
        }

        public void Dispose()
        {
            if (this.buffer == null)
                return;

            for (int i = 0; i < this.buffer.Queues.Length; i++)
            {
                try
                {
                    this.buffer.Queues[i]?.Dispose();
                    this.buffer.Queues[i] = null;
                }
                catch { }
            }
        }

        private bool ReadFirst()
        {
            this.buffer = RelationCache.CreateBuffer(this.Relation);
            this.buffer.Writer.Initializer(this.buffer);

            this.currentIndex = 0;

            return (this.readFactory = this.ReadNext)();
        }

        private bool ReadNext()
        {
            Action<RelationBuffer>[] writers = this.Buffer.Writer.Queues;
            IRelationQueue[] queues = this.Buffer.Queues;

            while (this.currentIndex >= 0)
            {
                if (this.currentIndex == writers.Length)
                {
                    this.currentIndex--;

                    return true;
                }
                else if (this.ReadOrThrow(queues[this.currentIndex]))
                {
                    writers[this.currentIndex](this.Buffer);

                    this.currentIndex++;
                }
                else
                    this.currentIndex--;
            }

            this.hasCompleted = true;

            return false;
        }

        public bool Read() => this.readFactory();

        private bool ReadOrThrow(IRelationQueue queue)
        {
            try
            {
                return queue.Read();
            }
            catch (Exception ex)
            {
                throw RelationException.CannotForwardQueue(this.Relation, queue.Metadata.Identity, ex);
            }
        }

        public IEnumerator<IField> GetEnumerator()
            => ((IEnumerable<IField>)this.Buffer.Fields).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => this.GetEnumerator();

        public override string ToString() => Tuple.Format(this);
    }
}
