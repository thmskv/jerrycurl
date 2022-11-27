using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jerrycurl.Tools.Orm;

namespace Jerrycurl.Tools.DotNet.Cli.Commands
{
    internal static class CommandExtensions
    {
        public static T GetValue<T>(this InvocationContext context, Option<T> option) => context.BindingContext.ParseResult.GetValueForOption(option);
        public static T GetValue<T>(this InvocationContext context, Argument<T> option) => context.BindingContext.ParseResult.GetValueForArgument(option);
        public static bool IsImplicit(this InvocationContext context, Option option)
        {
            OptionResult result = context.BindingContext.ParseResult.FindResultFor(option);

            return (result == null || result.IsImplicit);
        }

        public static bool IsExplicit(this InvocationContext context, Option option) => !context.IsImplicit(option);

        public static void Add(this Command command, params Option[] options)
        {
            foreach (var option in options)
                command.Add(option);
        }
    }
}
