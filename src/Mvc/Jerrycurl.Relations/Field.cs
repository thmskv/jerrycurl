using Jerrycurl.Diagnostics;
using Jerrycurl.Relations.Internal;
using Jerrycurl.Relations.Metadata;
using System;
using System.Diagnostics;
using HashCode = Jerrycurl.Diagnostics.HashCode;

namespace Jerrycurl.Relations
{
    [DebuggerDisplay("{Identity.Name}: {ToString(),nq}")]
    internal class Field<TValue, TParent> : IField
    {
        public FieldIdentity Identity { get; }
        public IField Model { get; }
        public FieldType Type { get; } = FieldType.Value;
        public IRelationMetadata Metadata { get; }
        public FieldData<TValue, TParent> Data { get; }
        public bool HasChanged { get; private set; }
        public object Snapshot { get; private set; }
        public bool IsReadOnly { get; }

        IFieldData IField.Data => this.Data;

        public Field(string name, IRelationMetadata metadata, FieldData<TValue, TParent> data, IField model, bool isReadOnly)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (metadata == null)
                throw new ArgumentNullException(nameof(metadata));

            this.Identity = new FieldIdentity(metadata.Identity, name);
            this.Model = model ?? throw new ArgumentNullException(nameof(model));
            this.Metadata = metadata;
            this.Data = data ?? throw new ArgumentNullException(nameof(data));
            this.Snapshot = data.Value;
            this.IsReadOnly = isReadOnly;
        }

        public void Update(object value)
        {
            this.Snapshot = value;
            this.HasChanged = true;
        }

        public void Commit()
        {
            if (!this.HasChanged)
                return;

            try
            {
                TValue typedValue = (TValue)this.Snapshot;

                this.Data.Bind((TValue)this.Snapshot);
                this.HasChanged = false;
            }
            catch (NotIndexableException)
            {
                throw BindingException.From(this, "Property has no indexer.");
            }
            catch (NotWritableException)
            {
                throw BindingException.From(this, "Property has no setter.");
            }
            catch (NullReferenceException)
            {
                throw BindingException.From(this, "Property cannot be null.");
            }
            catch (Exception ex)
            {
                throw BindingException.From(this, innerException: ex);
            }
        }

        public void Rollback()
        {
            if (!this.HasChanged)
                return;

            this.Snapshot = this.Data.Value;
            this.HasChanged = false;
        }

        public override string ToString() => this.Snapshot != null ? this.Snapshot.ToString() : "<null>";

        #region " Equality "
        public bool Equals(IField other) => Equality.Combine(this, other, m => m.Model, m => m.Identity);
        public override bool Equals(object obj) => (obj is IField other && this.Equals(other));
        public override int GetHashCode() => HashCode.Combine(this.Model, this.Identity);
        #endregion
    }
}
