using System;

namespace Jerrycurl.Tools
{
    public class ToolException : Exception
    {
        public int ErrorCode { get; }
        public string StdErr { get; }
        public string StdOut { get; }

        public ToolException(int errorCode, string message = null, string stdOut = null, string stdErr = null, Exception innerException = null)
            : base(message ?? innerException?.Message ?? $"Error {errorCode}", innerException)
        {
            this.ErrorCode = errorCode;
            this.StdErr = stdErr;
            this.StdOut = stdOut;
        }
    }
}
