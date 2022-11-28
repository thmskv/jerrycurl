using System;
using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.Tools
{
    public class ToolException : Exception
    {
        public int ErrorCode { get; set; }

        public ToolException(string message, int errorCode, Exception innerException)
            : base(message, innerException)
        {
            this.ErrorCode = errorCode;
        }
    }
}
