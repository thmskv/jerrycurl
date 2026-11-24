using System;

namespace Jerrycurl.Razor;

[Flags]
public enum DialectSupport
{
    None = 0,
    InputParameters = 1,
    OutputParameters = 2,
}
