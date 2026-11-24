using System;

namespace Jerrycurl.Mvc;

public interface ISqlPage
{
    public void Render();
    public void Throw(Exception ex);
}
