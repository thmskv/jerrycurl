using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations;
using System;
using System.Linq.Expressions;
using System.Runtime.Serialization;

namespace Jerrycurl.Mvc.Projections
{
    [Serializable]
    public class ProjectionException : Exception
    {
        public ProjectionException()
        {

        }

        public ProjectionException(string message)
            : base(message)
        {

        }

        public ProjectionException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        protected ProjectionException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {

        }

        #region " Exception helpers "

        internal static ProjectionException ArgumentNull(string argumentName, IProjectionMetadata metadata)
            => InvalidProjection(metadata, message: $"Argument '{argumentName}' cannot be null.");

        internal static ProjectionException InvalidLambda(IProjectionMetadata metadata, LambdaExpression expression)
            => new ProjectionException($"Cannot navigate from {metadata.Identity}: Expression '{expression}' is not valid.");

        internal static ProjectionException InvalidProjection(IProjectionMetadata metadata, string message)
        {
            if (metadata != null)
                return new ProjectionException($"Cannot create projection from {metadata.Identity}: {message}");
            else
                return new ProjectionException($"Cannot create projection: {message}");
        }

        internal static ProjectionException IdentityNotFound(IProjectionMetadata metadata)
            => new ProjectionException($"No identity attribute found for {metadata.Identity}.");

        internal static ProjectionException AttributesNotFound(IProjectionMetadata metadata)
            => new ProjectionException($"No attributes found for {metadata.Identity}.");

        internal static ProjectionException ValueNotFound(IProjectionMetadata metadata)
            => new ProjectionException($"No value information found for {metadata.Identity}.");

        internal static ProjectionException ValueNotFound(IField field)
            => new ProjectionException($"No value found for {field.Identity.Schema}({field.Identity}).");

        internal static ProjectionException TableNotFound(IProjectionMetadata metadata)
            => new ProjectionException($"No table information found for {metadata.Identity}.");

        internal static ProjectionException ColumnNotFound(IProjectionMetadata metadata)
            => new ProjectionException($"No column information found for {metadata.Identity}.");

        internal static ProjectionException JsonNotFound(IProjectionMetadata metadata)
            => new ProjectionException($"No JSON information found for {metadata.Identity}.");

        internal static ProjectionException ParametersNotSupported(IProjectionAttribute attribute)
            => new ProjectionException($"Cannot create parameter for {attribute.Metadata.Identity}: {attribute.Context.Domain.Dialect.GetType().Name} does not support input parameters.");

        internal static ProjectionException PropertyNotFound(IProjectionMetadata metadata)
            => new ProjectionException($"No property information found for {metadata.Identity}.");

        #endregion
    }
}
