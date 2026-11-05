using System.CommandLine;

namespace Jerrycurl.Tools.DotNet.Cli.Commands;

internal static class CommandExtensions
{
    public static void Add(this Command command, params Option[] options)
    {
        foreach (var option in options)
            command.Add(option);
    }
}
