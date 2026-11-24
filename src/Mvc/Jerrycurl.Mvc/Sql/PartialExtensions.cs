using System;
using System.Linq.Expressions;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Mvc.Sql;

public static class PartialExtensions
{
    /// <summary>
    /// Renders the page <typeparamref name="TPage"/> in place with <paramref name="model"/> as its model, writing its results into <paramref name="projection"/>.
    /// </summary>
    public static ISqlContent Subquery<TPage>(this IProjection projection, object model = null)
        where TPage : ISqlPage
        => projection.Subquery(typeof(TPage), model);

    public static ISqlContent Subquery<TModel, TProperty>(this IProjection<TModel> projection, Expression<Func<TModel, TProperty>> expression, Type pageType, object model = null)
        => projection.For(expression).Subquery(pageType, model);

    public static ISqlContent Subquery(this IProjection projection, Type pageType, object model = null)
    {
        ArgumentNullException.ThrowIfNull(pageType);

        ISchema modelSchema = projection.Context.Domain.Schemas.GetSchema(model?.GetType() ?? typeof(object));
        IProjectionMetadata modelMetadata = modelSchema.Require<IProjectionMetadata>();
        IField field = new Model(modelSchema, model);

        ProjectionIdentity modelIdentity = new ProjectionIdentity(field);

        IProjection modelProjection = new Projection(modelIdentity, projection.Context, modelMetadata);
        IProjection resultProjection = projection;

        return projection.Context.Renderer.Partial(pageType, modelProjection, resultProjection);
    }

    /// <summary>
    /// Renders the page <typeparamref name="TPage"/> in place with <paramref name="projection"/> as its model.
    /// </summary>
    public static ISqlContent Subcommand<TPage>(this IProjection projection)
        where TPage : ISqlPage
        => projection.Subcommand(typeof(TPage));

    public static ISqlContent Subcommand<TModel, TProperty>(this IProjection<TModel> projection, Expression<Func<TModel, TProperty>> expression, Type pageType)
        => projection.For(expression).Subcommand(pageType);

    public static ISqlContent Subcommand(this IProjection projection, Type pageType)
    {
        ArgumentNullException.ThrowIfNull(pageType);

        ISchema resultSchema = projection.Context.Domain.Schemas.GetSchema(typeof(object));
        IProjectionMetadata resultMetadata = resultSchema.Require<IProjectionMetadata>();
        ProjectionIdentity resultIdentity = new ProjectionIdentity(resultSchema);

        IProjection modelProjection = projection;
        IProjection resultProjection = new Projection(resultIdentity, projection.Context, resultMetadata);

        return projection.Context.Renderer.Partial(pageType, modelProjection, resultProjection);
    }
}
