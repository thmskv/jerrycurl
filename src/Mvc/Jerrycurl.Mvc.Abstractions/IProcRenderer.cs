using System;
using Jerrycurl.Mvc.Projections;

namespace Jerrycurl.Mvc;

public interface IProcRenderer
{
    public ISqlContent Body();
    public ISqlContent Partial(Type pageType, IProjection model, IProjection result);
}
