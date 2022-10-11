using Jerrycurl.Diagnostics;
using Jerrycurl.Relations.Metadata;
using System;
using System.Diagnostics;

namespace Jerrycurl.Relations
{
    [DebuggerDisplay("{Identity.Name}: {ToString(),nq}")]
    public class Model : IField
    {
        public FieldIdentity Identity { get; }
        public object Snapshot { get; }
        public FieldType Type { get; } = FieldType.Model;
        public IRelationMetadata Metadata { get; }
        public bool HasChanged => false;
        public IFieldData Data { get; }
        public bool IsReadOnly => true;

        IField IField.Model => this;

        public Model(ISchema schema, object value)
        {
            if (schema == null)
                throw new ArgumentNullException(nameof(schema));

            this.Metadata = schema.Require<IRelationMetadata>();
            this.Identity = new FieldIdentity(this.Metadata.Identity, this.Metadata.Identity.Name);
            this.Snapshot = value;
            this.Data = new FieldData(value);
        }

        public void Commit() { }
        public void Rollback() { }
        public void Update(object model) => throw BindingException.From(this, "Cannot update model field.");

        public override string ToString() => this.Snapshot != null ? this.Snapshot.ToString() : "<null>";

        #region " Equality "
        public bool Equals(IField other) => Equality.Combine(this, other, m => m.Identity, m => m.Snapshot);
        public override bool Equals(object obj) => (obj is IField other && this.Equals(other));
        public override int GetHashCode() => this.Identity.GetHashCode();
        #endregion

    }
}
