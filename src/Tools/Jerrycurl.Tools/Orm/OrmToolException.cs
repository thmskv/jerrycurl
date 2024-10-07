using System;
using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Tools.Orm;

public class OrmToolException : Exception
{
    public string Log { get; }
    public string Type { get; set; }

    public OrmToolException(string message)
        : base(message)
    {

    }

    public OrmToolException(string message, Exception innerException = null, string log = null, string type = null)
        : base(message, innerException)
    {
        this.Log = log;
        this.Type = type;
    }
}
