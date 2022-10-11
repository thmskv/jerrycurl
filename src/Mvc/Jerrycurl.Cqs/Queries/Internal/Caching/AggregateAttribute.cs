using System;
using Jerrycurl.Diagnostics;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Cqs.Queries.Internal.Caching
{
    internal class AggregateAttribute : DataAttribute, IEquatable<AggregateAttribute>
    {
        public int? AggregateIndex { get; set; }
        public int? ListIndex { get; set; }

        public AggregateAttribute(string name, int? aggregateIndex, int? listIndex)
            : base(name)
        {
            this.AggregateIndex = aggregateIndex;
            this.ListIndex = listIndex;
        }

        public bool Equals(AggregateAttribute other) => Equality.Combine(this, other, m => m.AggregateIndex, m => m.ListIndex);
        public override bool Equals(object obj) => (obj is AggregateAttribute other && this.Equals(other));
        public override int GetHashCode() => HashCode.Combine(this.AggregateIndex, this.ListIndex);
    }
}
