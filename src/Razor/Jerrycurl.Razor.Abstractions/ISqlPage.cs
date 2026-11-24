using System;

namespace Jerrycurl.Razor;

public interface ISqlPage
{
    public void Render();
    public void Throw(Exception ex);
}
