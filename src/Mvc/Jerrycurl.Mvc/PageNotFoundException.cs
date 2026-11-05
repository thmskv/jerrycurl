using System;

namespace Jerrycurl.Mvc;

public class PageNotFoundException : Exception
{
    public PageNotFoundException()
    {

    }

    public PageNotFoundException(string message)
        : base(message)
    {

    }

    public PageNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {

    }

}
