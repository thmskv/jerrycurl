using System.Data;

namespace Jerrycurl.Cqs.Filters
{
    public interface IFilter
    {
        IFilterHandler GetHandler(IDbConnection connection);
        IFilterAsyncHandler GetAsyncHandler(IDbConnection connection);
    }
}
