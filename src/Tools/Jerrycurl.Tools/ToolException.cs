using System;

namespace Jerrycurl.Tools;

public class ToolException : Exception
{
    public int ExitCode { get; }
    public string StdErr { get; }
    public string StdOut { get; }

    public ToolException(int exitCode = -1, string message = null, string stdOut = null, string stdErr = null, Exception innerException = null)
        : base(message ?? innerException?.Message ?? $"Error {exitCode}", innerException)
    {
        this.StdErr = stdErr;
        this.StdOut = stdOut;
    }
}
