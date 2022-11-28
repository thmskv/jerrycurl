using System.CommandLine;

namespace Jerrycurl.Tools.DotNet.Cli.Commands
{
    internal interface ICommandBuilder
    {
        void Build(RootCommand command);
    }
}
