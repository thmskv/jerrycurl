using System;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations
{
    public interface IField : IEquatable<IField>
    {
        FieldIdentity Identity { get; }
        object Snapshot { get; }
        IField Model { get; }
        FieldType Type { get; }
        IRelationMetadata Metadata { get; }
        IFieldData Data { get; }
        bool HasChanged { get; }
        bool IsReadOnly { get; }

        void Commit();
        void Rollback();
        void Update(object value);
    }
}
