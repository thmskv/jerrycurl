using System;
using System.Linq;
using System.Linq.Expressions;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;

namespace Jerrycurl.Mvc.Sql
{
    public static class BoolExtensions
    {
        /// <summary>
        /// Determines whether or not the current projection contains any attributes.
        /// </summary>
        /// <param name="projection">The current projection</param>
        /// <returns><see langword="true"/> if the projection contains attributes; otherwise <see langword="false"/>.</returns>
        public static bool Any(this IProjection projection) => projection.Header.Any();


        public static bool HasVal(this IProjection projection) => (projection.Data.Source.Snapshot != null);
        public static bool HasVal(this IProjectionAttribute attribute) => (attribute.Data.Source.Snapshot != null);
        public static bool HasVal<TModel, TProperty>(this IProjection<TModel> projection, Expression<Func<TModel, TProperty>> expression)
            => projection.Attr(expression).HasVal();

        public static bool HasId(this IProjection projection) => projection.Header.Any(a => a.Metadata.HasFlag(ProjectionMetadataFlags.Identity));
    }
}
