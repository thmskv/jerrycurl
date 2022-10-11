using System;
using Jerrycurl.Diagnostics;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Cqs.Queries.Internal.Caching
{
    internal class ColumnAttribute : DataAttribute, IEquatable<ColumnAttribute>
    {
        public Type Type { get; }
        public int Index { get; }
        public string TypeName { get; }

        public ColumnAttribute(string name, int index, Type type, string typeName)
            : base(name)
        {
            this.Index = index;
            this.Type = type;
            this.TypeName = typeName;
        }

        public bool Equals(ColumnAttribute other) => Equality.Combine(this, other, m => m.Name, m => m.Type, m => m.TypeName, m => m.Index);
        public override bool Equals(object obj) => (obj is ColumnAttribute other && this.Equals(other));
        public override int GetHashCode() => HashCode.Combine(this.Name, this.Type, this.TypeName, this.Index);
    }
}
