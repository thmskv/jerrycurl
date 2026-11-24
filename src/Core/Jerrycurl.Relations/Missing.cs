using Jerrycurl.Diagnostics;
using Jerrycurl.Relations.Metadata;
using System;
using System.Diagnostics;

namespace Jerrycurl.Relations;

[DebuggerDisplay("{Identity.Name}: {ToString(),nq}")]
internal class Missing<TValue> : IField
{
    public FieldIdentity Identity { get; }
    public IField Model { get; }
    public FieldType Type { get; } = FieldType.Missing;
    public IRelationMetadata Metadata { get; }
    public bool HasChanged => false;
    public IFieldData Data { get; }
    public object Snapshot => null;
    public bool IsReadOnly => true;

    public Missing(string name, IRelationMetadata metadata, FieldData data, IField model)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(metadata);

        this.Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        this.Identity = new FieldIdentity(metadata.Identity, name);
        this.Model = model ?? throw new ArgumentNullException(nameof(model));
        this.Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    public void Commit() { }
    public void Rollback() { }
    public void Update(object value) => throw BindingException.From(this, "Cannot update missing field.");

    public override string ToString() => "<missing>";

    #region " Equality "
    public bool Equals(IField other) => Equality.Combine(this, other, m => m.Model, m => m.Identity);
    public override bool Equals(object obj) => (obj is IField other && this.Equals(other));
    public override int GetHashCode() => HashCode.Combine(this.Model, this.Identity);
    #endregion
}
