using System;
using System.Collections.Generic;

namespace Jerrycurl.Tools;

public class ToolRunnerOptions
{
    public string ToolName { get; set; }
    public IEnumerable<string> Arguments { get; set; }
    public string WorkingDirectory { get; set; }
    public Action<string> StdErr { get; set; }
    public Action<string> StdOut { get; set; }
    public int Timeout { get; set; } = 30_000;
}
