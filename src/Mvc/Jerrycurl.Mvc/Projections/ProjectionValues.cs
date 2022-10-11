using System;
using System.Collections;
using System.Collections.Generic;

namespace Jerrycurl.Mvc.Projections
{
    public class ProjectionValues<TItem> : IProjectionValues<TItem>
    {
        public ProjectionIdentity Identity { get; }
        public IProcContext Context { get; }
        public IEnumerable<IProjection<TItem>> Items { get; }
        public int BatchIndex { get; }

        public ProjectionValues(IProcContext context, ProjectionIdentity identity, IEnumerable<IProjection<TItem>> items, int batchIndex = -1)
        {
            this.Context = context ?? throw new ArgumentNullException(nameof(context));
            this.Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.Items = items ?? throw new ArgumentNullException(nameof(identity));
            this.BatchIndex = batchIndex;

            if (this.Items is IProjectionValues<TItem> innerValues)
                this.Items = innerValues.Items;
        }

        public IEnumerator<IProjection<TItem>> GetEnumerator()
        {
            this.Context.Execution.Buffer.Push(this.BatchIndex);

            foreach (IProjection<TItem> item in this.Items)
            {
                yield return item;

                this.Context.Execution.Buffer.Mark();
            }

            this.Context.Execution.Buffer.Pop();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
