using System;

namespace Jerrycurl.Cqs.Metadata
{
    public sealed class ColumnMetadata
    {
        public string Name { get; }
        public Type Type { get; }
        public int Index { get; }
        public string TypeName { get; }

        public ColumnMetadata(string name, Type type, string typeName, int index)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
            this.Type = type;
            this.TypeName = typeName;
            this.Index = index;
        }
    }
}
