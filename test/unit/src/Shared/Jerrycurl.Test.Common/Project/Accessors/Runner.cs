using System;
using System.Collections.Generic;
using Jerrycurl.Razor;
using Jerrycurl.Razor.Projections;
using Jerrycurl.Test.Project.Commands;
using Jerrycurl.Test.Project.Models;
using Jerrycurl.Test.Project.Queries;

namespace Jerrycurl.Test.Project.Accessors;

/// <summary>
/// Runs ad-hoc <see cref="Runnable{TModel, TResult}"/> pages through <see cref="RunnerQuery"/> and <see cref="RunnerCommand"/>.
/// </summary>
public class Runner
{
    public static TResult Aggregate<TModel, TResult>(Runnable<TModel, TResult> runner) => RunnerQuery.Aggregate<TResult>(runner);
    public static IList<TResult> Query<TModel, TResult>(Runnable<TModel, TResult> runner) => RunnerQuery.Query<TResult>(runner);
    public static void Command<TModel, TResult>(Runnable<TModel, TResult> runner) => RunnerCommand.Execute(runner);

    public string Sql<TModel, TResult>(Runnable<TModel, TResult> model) => RunnerQuery.Sql(model, typeof(List<TResult>));

    public string Sql<TModel>(TModel model, Func<IProjection<TModel>, ISqlWritable> func)
    {
        Runnable<TModel, object> runnable = new Runnable<TModel, object>(model);

        runnable.M(p => func(p.With(options: new ProjectionOptions(p.Options) { Separator = "," })));

        return this.Sql(runnable);
    }

    public string Sql<TResult>(Func<IProjection<TResult>, ISqlWritable> func)
    {
        Runnable<object, TResult> runnable = new Runnable<object, TResult>();

        runnable.R(p => func(p.With(options: new ProjectionOptions(p.Options) { Separator = "," })));

        return this.Sql(runnable);
    }
}
