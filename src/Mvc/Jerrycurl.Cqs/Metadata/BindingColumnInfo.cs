namespace Jerrycurl.Cqs.Metadata
{
    internal class BindingColumnInfo : IBindingColumnInfo
    {
        public ColumnMetadata Column { get; set; }
        public IBindingMetadata Metadata { get; set; }
        public bool CanBeNull { get; set; }
    }
}
