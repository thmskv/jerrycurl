using Jerrycurl.Mvc.Projections;
using System;
using System.Linq.Expressions;
using Jerrycurl.Relations;
using Jerrycurl.Vendors.SqlServer;
using System.Linq;

namespace Jerrycurl.Mvc.Sql.SqlServer
{
    public static class TvpExtensions
    {
        /// <summary>
        /// Appends a table-valued parameter from the current values, e.g. <c>@TP0</c>, to the projection buffer.
        /// </summary>
        /// <param name="projection">The current projection.</param>
        /// <returns>A new attribute containing the appended buffer.</returns>
        public static IProjectionAttribute TvpName(this IProjection projection)
        {
            if (projection.Data == null)
                throw new ProjectionException($"No value information found for {projection.Metadata.Identity}.");
            else if (!projection.Any())
                throw new ProjectionException($"No attributes found for {projection.Metadata.Identity}.");

            RelationHeader header = new RelationHeader(projection.Metadata.Identity.Schema, projection.Header.Select(a => a.Metadata.Relation).ToList());
            Relation relation = new Relation(projection.Data.Input, header);

            string paramName = projection.Context.Lookup.Custom("TP", projection.Identity, metadata: projection.Metadata.Identity, value: projection.Data.Input);
            string dialectName = projection.Context.Domain.Dialect.Parameter(paramName);

            return projection.Attr().Append(dialectName).Append(new TableValuedParameter(paramName, relation));
        }

        /// <summary>
        /// Appends a table-valued parameter from the current values, e.g. <c>@TP0</c>, to the projection buffer.
        /// </summary>
        /// <param name="projection">The current projection.</param>
        /// <param name="expression">Expression selecting a specific attribute.</param>
        /// <returns>A new attribute containing the appended buffer.</returns>
        public static IProjectionAttribute TvpName<TModel, TProperty>(this IProjection<TModel> projection, Expression<Func<TModel, TProperty>> expression) => projection.For(expression).TvpName();

        /// <summary>
        /// Appends a correlated table-valued parameter from the selected values, e.g. <c>@TP0 T0</c>, to the projection buffer.
        /// </summary>
        /// <param name="projection">The current projection.</param>
        /// <param name="expression">Expression selecting a specific attribute.</param>
        /// <param name="tblAlias">The table alias to qualify each column name with.</param>
        /// <returns>A new attribute containing the appended buffer.</returns>
        public static IProjectionAttribute Tvp<TModel, TProperty>(this IProjection<TModel> projection, Expression<Func<TModel, TProperty>> expression, string tblAlias = null) => projection.For(expression).Tvp(tblAlias);

        /// <summary>
        /// Appends a correlated table-valued parameter from the current values, e.g. <c>@TP0 T0</c>, to the projection buffer.
        /// </summary>
        /// <param name="projection">The current projection.</param>
        /// <param name="tblAlias">The table alias to qualify each column name with.</param>
        /// <returns>A new attribute containing the appended buffer.</returns>
        public static IProjectionAttribute Tvp(this IProjection projection, string tblAlias = null)
        {
            IProjectionAttribute attribute = projection.TvpName().Append(" ");

            if (tblAlias != null)
                return attribute.Append(attribute.Context.Domain.Dialect.Identifier(tblAlias));
            else
                return attribute.Ali();
        }
    }
}
