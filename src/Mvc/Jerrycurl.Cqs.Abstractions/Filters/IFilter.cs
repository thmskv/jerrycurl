using System.Data;

namespace Jerrycurl.Cqs.Filters;

public interface IFilter
{
    public IFilterHandler GetHandler(IDbConnection connection);
    public IFilterAsyncHandler GetAsyncHandler(IDbConnection connection);
}
