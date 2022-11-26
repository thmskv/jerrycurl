using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jerrycurl.Tools.DotNet.Cli.Commands
{
    internal interface ICommandBuilder
    {
        void Build(RootCommand command);
    }
}
