namespace Jerrycurl.Cqs.Metadata;

public interface ITableNamingPolicy
{
    public string[] GetTableName(ITableMetadata metadata);
    public string GetColumnName(ITableMetadata metadata);
}
