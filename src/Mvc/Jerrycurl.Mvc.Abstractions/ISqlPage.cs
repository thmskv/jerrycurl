using System;

namespace Jerrycurl.Mvc;

public interface ISqlPage
{
    public void Execute();
    public void Throw(Exception ex);
}
