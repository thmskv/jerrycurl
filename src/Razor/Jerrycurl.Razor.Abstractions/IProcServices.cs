using Jerrycurl.Razor.Projections;

namespace Jerrycurl.Razor;

public interface IProcServices
{
    public TService GetService<TService>() where TService : class;
    public IProjection<TModel> GetProjection<TModel>(IProcContext context);
}
