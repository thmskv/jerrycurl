using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Mvc.Metadata;
using System;
using System.Linq.Expressions;

namespace Jerrycurl.Mvc.Projections
{
    internal static class ProjectionHelper
    {
        public static ITableMetadata GetTableMetadata(IProjection projection) => GetTableMetadata(projection.Metadata);
        public static ITableMetadata GetTableMetadata(IProjectionAttribute attribute) => GetTableMetadata(attribute.Metadata);
        public static ITableMetadata GetTableMetadata(IProjectionMetadata metadata) => GetPreferredTableMetadata(metadata)?.Table;
        public static ITableMetadata GetColumnMetadata(IProjectionAttribute attribute)
            => attribute.Metadata.Column ?? attribute.Metadata.Item?.Column ?? throw ProjectionException.ColumnNotFound(attribute.Metadata);

        public static IProjectionMetadata GetPreferredTableMetadata(IProjectionMetadata metadata)
        {
            if (metadata.Table != null)
                return metadata;
            else if (metadata.Item?.Table != null)
                return metadata.Item;

            throw ProjectionException.TableNotFound(metadata);
        }

        public static IJsonMetadata GetJsonMetadata(IProjectionMetadata metadata)
            => metadata.Identity.Lookup<IJsonMetadata>() ?? throw ProjectionException.JsonNotFound(metadata);

        public static IProjectionMetadata GetMetadataFromRelativeLambda(IProjection projection, LambdaExpression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            string name = projection.Metadata.Identity.Notation.Lambda(expression) ?? throw ProjectionException.InvalidLambda(projection.Metadata, expression);
            string fullName = projection.Metadata.Identity.Notation.Combine(projection.Metadata.Identity.Name, name);

            return projection.Metadata.Identity.Schema.Require<IProjectionMetadata>(fullName);
        }
    }
}
