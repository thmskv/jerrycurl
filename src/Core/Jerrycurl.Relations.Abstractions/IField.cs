using System;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations;

public interface IField : IEquatable<IField>
{
    public FieldIdentity Identity { get; }
    public object Snapshot { get; }
    public IField Model { get; }
    public FieldType Type { get; }
    public IRelationMetadata Metadata { get; }
    public IFieldData Data { get; }
    public bool HasChanged { get; }
    public bool IsReadOnly { get; }

    public void Commit();
    public void Rollback();
    public void Update(object value);
}
