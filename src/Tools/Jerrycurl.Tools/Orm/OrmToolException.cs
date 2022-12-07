using System;
using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Tools.Orm
{
    public class OrmToolException : Exception
    {
        public string Log { get; }

        public OrmToolException(string message)
            : base(message)
        {

        }

        public OrmToolException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        public OrmToolException(string message, string log, Exception innerException)
            : base(message, innerException)
        {
            this.Log = log;
        }
    }
}
