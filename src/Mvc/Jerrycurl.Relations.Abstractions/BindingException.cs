using System;
using System.Runtime.Serialization;

namespace Jerrycurl.Relations
{
    [Serializable]
    public class BindingException : Exception
    {
        public BindingException()
        {

        }

        public BindingException(string message)
            : base(message)
        {

        }

        public BindingException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected BindingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        #region " Exception helpers "

        public static BindingException From(IField field, string message = null, Exception innerException = null)
        {
            string fullMessage = $"Error binding to {field.Identity} in {field.Metadata.Identity.Schema}.";

            if (message != null || innerException != null)
                fullMessage += $" {message ?? innerException.Message}";

            return new BindingException(fullMessage, innerException);
        }

        #endregion
    }
}
