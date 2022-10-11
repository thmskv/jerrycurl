using System;
using System.Collections.Generic;
using Jerrycurl.Cqs.Queries.Internal;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Internal.Queues
{
    internal class RelationQueue<TList, TItem> : IRelationQueue
        where TList : IEnumerable<TItem>
    {
        private IEnumerator<TItem> innerEnumerator;
        private IEnumerator<FieldArray> cacheEnumerator;

        public TList List => this.CurrentItem.List;
        public TItem Current => this.innerEnumerator.Current;
        public RelationQueueItem<TList> CurrentItem => this.innerQueue.Peek();
        public int Index => this.CurrentItem.Index;
        public RelationQueueType Type { get; }
        public IRelationMetadata Metadata { get; }
        public FieldArray Cache { get; private set; }
        public bool IsCached { get; private set; }

        private Queue<RelationQueueItem<TList>> innerQueue = new Queue<RelationQueueItem<TList>>();

        public RelationQueue(IRelationMetadata metadata, RelationQueueType queueType)
        {
            this.Metadata = metadata;
            this.Type = queueType;
        }

        public void Enqueue(RelationQueueItem<TList> item)
        {
            if (this.IsCached)
            {
                this.IsCached = false;
                this.innerQueue.Clear();
                this.Reset();
            }

            this.innerQueue.Enqueue(item);
        }

        private void Start()
        {
            if (this.HasItems)
            {
                if (this.IsCached)
                    this.cacheEnumerator = this.CurrentItem.Cache.GetEnumerator();
                else
                    this.innerEnumerator = (this.CurrentItem.List ?? (IEnumerable<TItem>)Array.Empty<TItem>()).GetEnumerator();
            }
        }

        private bool IsStarted => this.IsCached ? this.cacheEnumerator != null : this.innerEnumerator != null;
        private bool HasItems => (this.innerQueue.Count > 0);

        private bool MoveNext()
        {
            if (!this.IsStarted)
                this.Start();

            bool result;

            if (!this.IsStarted)
                result = false;
            else if (this.IsCached)
            {
                if (result = this.cacheEnumerator.MoveNext())
                    this.Cache = this.cacheEnumerator.Current;
            }
            else if (this.Type == RelationQueueType.Cached)
            {
                if (result = this.innerEnumerator.MoveNext())
                {
                    this.CurrentItem.Increment();
                    this.CurrentItem.Cache.Add(this.Cache = new FieldArray());
                }
            }
            else
            {
                if (result = this.innerEnumerator.MoveNext())
                    this.CurrentItem.Increment();
            }

            return result;
        }

        public string GetFieldName(string namePart) => this.CurrentItem.CombineWith(namePart);

        public bool Read()
        {
            if (this.MoveNext())
                return true;

            this.Dequeue();

            if (this.Type == RelationQueueType.Recursive && this.HasItems)
                return this.Read();

            return false;
        }

        private void Reset()
        {
            this.innerEnumerator?.Dispose();
            this.innerEnumerator = null;

            this.Cache = null;
            this.cacheEnumerator?.Dispose();
            this.cacheEnumerator = null;
        }

        private void Dequeue()
        {
            if (this.Type == RelationQueueType.Cached)
            {
                this.IsCached = true;
                this.Reset();
            }
            else if (this.HasItems)
            {
                this.innerQueue.Dequeue();
                this.Reset();
            }
        }

        public void Dispose() => this.Reset();
    }
}
