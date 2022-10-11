namespace Jerrycurl.Cqs.Metadata
{
    public interface IBindingColumnInfo
    {
        ColumnMetadata Column { get; }
        IBindingMetadata Metadata { get; }
        bool CanBeNull { get; }
    }
}