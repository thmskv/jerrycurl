using System;

namespace Jerrycurl.Cqs.Metadata.Annotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        public string Name { get; }

        public ColumnAttribute()
        {

        }

        public ColumnAttribute(string name)
        {
            this.Name = name;
        }
    }
}
