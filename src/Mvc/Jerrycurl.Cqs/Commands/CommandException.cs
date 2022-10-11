using System;
using System.Runtime.Serialization;

namespace Jerrycurl.Cqs.Commands
{
    [Serializable]
    public class CommandException : Exception
    {
        public CommandException()
        {

        }

        public CommandException(string message)
            : base(message)
        {

        }

        public CommandException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected CommandException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        internal static CommandException NoSchemaStoreAttached()
            => new CommandException("No schema store attached; use the CommandBuffer(ISchemaStore) constructor to enable language features.");

        internal static CommandException InvalidBindingType(IUpdateBinding binding)
            => new CommandException($"Invalid binding type '{binding.GetType().Name}': Only ColumnBinding, ParameterBinding and CascadeBinding are supported.");

    }
}
