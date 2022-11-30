using System;

namespace Jerrycurl.Tools
{
    public class ToolException : Exception
    {
        public int ErrorCode { get; set; }

        public ToolException(string message, int errorCode, Exception innerException = null)
            : base(message, innerException)
        {
            this.ErrorCode = errorCode;
        }
    }
}
