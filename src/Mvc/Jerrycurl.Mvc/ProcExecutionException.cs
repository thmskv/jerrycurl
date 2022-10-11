using System;
using System.Runtime.Serialization;
using Jerrycurl.Reflection;

namespace Jerrycurl.Mvc
{
    public class ProcExecutionException : Exception
    {
        public ProcExecutionException()
        {

        }

        public ProcExecutionException(string message)
            : base(message)
        {

        }

        public ProcExecutionException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected ProcExecutionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        internal static ProcExecutionException StackNotInitialized()
            => new ProcExecutionException("Execution stack is not initialized. Please add an IPageExecutionContext instance before accessing the stack.");

        internal static ProcExecutionException DomainNotFound(Type pageType)
            => throw new ProcExecutionException($"No domain found for page type '{pageType.GetSanitizedFullName()}'. Make sure to implement IDomain in a parent namespace.");

        internal static ProcExecutionException MustInheritProcPage()
            => new ProcExecutionException("Type must inherit ProcPage<TModel, TResult>.");
    }
}
