using System;
using Jerrycurl.Razor.Projections;

namespace Jerrycurl.Razor;

public interface IProcRenderer
{
    public ISqlContent Body();
    public ISqlContent Partial(Type pageType, IProjection model, IProjection result);
}
