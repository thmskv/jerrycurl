namespace Jerrycurl.Cqs.Metadata;

public interface IBindingColumnInfo
{
    public ColumnMetadata Column { get; }
    public IBindingMetadata Metadata { get; }
    public bool CanBeNull { get; }
}