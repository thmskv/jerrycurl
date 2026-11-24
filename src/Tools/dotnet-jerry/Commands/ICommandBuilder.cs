using System.CommandLine;

namespace Jerrycurl.Tools.DotNet.Cli.Commands;

internal interface ICommandBuilder
{
    public void Build(RootCommand command);
}
