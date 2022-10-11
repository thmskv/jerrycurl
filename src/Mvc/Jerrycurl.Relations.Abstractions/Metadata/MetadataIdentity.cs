using System;
using Jerrycurl.Diagnostics;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Relations.Metadata
{
    public sealed class MetadataIdentity : IEquatable<MetadataIdentity>
    {
        public string Name { get; }
        public ISchema Schema { get; }
        public DotNotation Notation => this.Schema.Notation;

        public MetadataIdentity(ISchema schema, string name)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public MetadataIdentity(ISchema schema)
            : this(schema, schema?.Notation?.Model())
        {

        }

        public TMetadata Lookup<TMetadata>() where TMetadata : IMetadata
            => this.Schema.Lookup<TMetadata>(this.Name);

        public TMetadata Require<TMetadata>() where TMetadata : IMetadata
            => this.Schema.Require<TMetadata>(this.Name);

        public override string ToString() => $"{this.Schema}(\"{this.Name}\")";

        public MetadataIdentity Pop()
        {
            string parentName = this.Notation.Parent(this.Name);

            if (parentName != null)
                return new MetadataIdentity(this.Schema, parentName);

            return null;
        }

        public MetadataIdentity Push(string propertyName) => new MetadataIdentity(this.Schema, this.Notation.Combine(this.Name, propertyName));

        #region " Equality "
        public bool Equals(string name)
            => this.Equals(new MetadataIdentity(this.Schema, name));

        public bool Equals(ISchema schema, string name)
            => this.Equals(new MetadataIdentity(schema, name));

        public bool Equals(MetadataIdentity other)
        {
            if (other == null)
                return false;

            Equality eq = new Equality();

            eq.Add(this.Name, other.Name, this.Notation.Comparer);
            eq.Add(this.Schema, other.Schema);

            return eq.ToEquals();
        }

        public override bool Equals(object obj) => (obj is MetadataIdentity other && this.Equals(other));
        public override int GetHashCode()
        {
            HashCode hashCode = new HashCode();

            hashCode.Add(this.Name, this.Notation.Comparer);
            hashCode.Add(this.Schema);

            return hashCode.ToHashCode();
        }
        #endregion
    }
}
