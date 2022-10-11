using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations.Metadata;
using Jerrycurl.Collections;
using Jerrycurl.Cqs.Sessions;

namespace Jerrycurl.Mvc.Sql
{
    public static class ParameterExtensions
    {
        public static IProjection IsEq(this IProjection projection, IProjection other)
        {
            List<IProjectionAttribute> newHeader = new List<IProjectionAttribute>();

            foreach (var (l, r) in projection.Attrs().Zip(other.Attrs()))
            {
                IProjectionAttribute newAttr = l;

                newAttr = newAttr.Eq();
                newAttr = newAttr.With(data: r.Data).Par();
                newAttr = newAttr.With(data: l.Data);

                newHeader.Add(newAttr);
            }

            ProjectionOptions newOptions = new ProjectionOptions(projection.Options)
            {
                Separator = Environment.NewLine + "AND" + Environment.NewLine,
            };

            return projection.With(header: newHeader, options: newOptions);
        }

        /// <summary>
        /// Appends the current parameter name and value, e.g. <c>@P0</c>, to the attribute buffer.
        /// </summary>
        /// <param name="attribute">The current attribute.</param>
        /// <returns>A new attribute containing the appended buffer.</returns>
        public static IProjectionAttribute Par(this IProjectionAttribute attribute)
        {
            if (!attribute.Context.Domain.Dialect.Support.HasFlag(DialectSupport.InputParameters))
                throw ProjectionException.ParametersNotSupported(attribute);

            if (attribute.Data?.Input != null)
            {
                string paramName = attribute.Context.Lookup.Parameter(attribute.Identity, attribute.Data.Input);
                string dialectName = attribute.Context.Domain.Dialect.Parameter(paramName);

                Parameter param = new Parameter(paramName, attribute.Data.Input);

                IProjectionAttribute result = attribute.Append(dialectName).Append(param);

                if (attribute.Metadata.HasFlag(ProjectionMetadataFlags.Output))
                {
                    if (attribute.Context.Domain.Dialect.Support.HasFlag(DialectSupport.OutputParameters))
                    {
                        ParameterBinding binding = new ParameterBinding(attribute.Data.Output, param.Name);

                        result = result.Append(binding);
                    }

                    if (attribute.Metadata.HasAnyFlag(ProjectionMetadataFlags.Cascade))
                    {
                        CascadeBinding binding = new CascadeBinding(attribute.Data.Output, attribute.Data.Input);

                        result = result.Append(binding);
                    }
                }

                return result;
            }
            else
            {
                string paramName = attribute.Context.Lookup.Parameter(attribute.Identity, attribute.Metadata.Identity);
                string dialectName = attribute.Context.Domain.Dialect.Parameter(paramName);

                return attribute.Append(dialectName);
            }
        }

        /// <summary>
        /// Appends the current parameter name and value, e.g. <c>@P0</c>, to a new attribute buffer.
        /// </summary>
        /// <param name="projection">The current projection.</param>
        /// <returns>A new attribute containing the appended buffer.</returns>
        public static IProjectionAttribute Par(this IProjection projection) => projection.Attr().Par();

        /// <summary>
        /// Appends the selected parameter name and value, e.g. <c>@P0</c>, to a new attribute buffer.
        /// </summary>
        /// <param name="projection">The current projection.</param>
        /// <param name="expression">Expression selecting a projection target.</param>
        /// <returns>A new attribute containing the appended buffer.</returns>
        public static IProjectionAttribute Par<TModel, TProperty>(this IProjection<TModel> projection, Expression<Func<TModel, TProperty>> expression)
            => projection.Attr(expression).Par();

        /// <summary>
        /// Appends the current parameter names and values, e.g. <c>@P0</c>, to the projection buffer.
        /// </summary>
        /// <param name="projection">The current projection.</param>
        /// <returns>A new projection containing the appended buffer.</returns>
        public static IProjection Pars(this IProjection projection) => projection.Map(a => a.Par());

        /// <summary>
        /// Appends the selected parameter names and values, e.g. <c>@P0</c>, to the projection buffer.
        /// </summary>
        /// <param name="projection">The current projection.</param>
        /// <param name="expression">Expression selecting a projection target.</param>
        /// <returns>A new projection containing the appended buffer.</returns>
        public static IProjection Pars<TModel, TProperty>(this IProjection<TModel> projection, Expression<Func<TModel, TProperty>> expression)
            => projection.For(expression).Pars();

        /// <summary>
        /// Appends a comma-separated list of parameter names and values, e.g. <c>@P0, @P1, @P2</c>, to a new attribute buffer.
        /// </summary>
        /// <param name="projection">The current projection.</param>
        /// <returns>A new attribute containing the appended buffer.</returns>
        public static IProjectionAttribute ParList(this IProjection projection) => projection.ValList(a => a.Par());

        /// <summary>
        /// Appends a comma-separated list of parameter names and values, e.g. <c>@P0, @P1, @P2</c>, to a new attribute buffer.
        /// </summary>
        /// <param name="projection">The current projection.</param>
        /// <param name="expression">Expression selecting a projection target.</param>
        /// <returns>A new attribute containing the appended buffer.</returns>
        public static IProjectionAttribute ParList<TModel, TItem>(this IProjection<TModel> projection, Expression<Func<TModel, IEnumerable<TItem>>> expression)
            => projection.Open(expression).ParList();
    }
}
