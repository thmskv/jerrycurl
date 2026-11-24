using Jerrycurl.Mvc.Projections;

namespace Jerrycurl.Mvc;

public interface IProcRenderer
{
    public ISqlContent Body();
    public ISqlContent Partial(string procName, IProjection model, IProjection result);
}
