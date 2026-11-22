namespace Jerrycurl.Cqs.Metadata;

public interface ITableNamingPolicy
{
    string[] GetTableName(ITableMetadata metadata);
    string GetColumnName(ITableMetadata metadata);
}
