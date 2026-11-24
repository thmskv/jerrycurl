using Jerrycurl.Mvc.Projections;

namespace Jerrycurl.Mvc;

public interface IProcServices
{
    public TService GetService<TService>() where TService : class;
    public IProjection<TModel> GetProjection<TModel>(IProcContext context);
}
