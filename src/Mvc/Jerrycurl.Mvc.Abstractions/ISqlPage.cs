using System;

namespace Jerrycurl.Mvc;

public interface ISqlPage
{
    void Execute();
    void Throw(Exception ex);
}
