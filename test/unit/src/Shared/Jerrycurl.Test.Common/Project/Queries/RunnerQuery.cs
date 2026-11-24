using System;
using System.Collections.Generic;
using Jerrycurl.Mvc;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Test.Project.Models;

namespace Jerrycurl.Test.Project.Queries;

/// <summary>
/// Renders any <see cref="IRunnable"/> as a query. Hand-written equivalent of a generated <c>@query</c> page.
/// </summary>
public class RunnerQuery : ProcPage<IRunnable, object>
{
    public RunnerQuery(IProjection model, IProjection result)
        : base(model, result)
    {

    }

    public override void Render() => this.Model.Execute(this);

    public static IList<TResult> Query<TResult>(IRunnable model) => QueryCore<TResult>(typeof(RunnerQuery), model);
    public static TResult Aggregate<TResult>(IRunnable model) => AggregateCore<TResult>(typeof(RunnerQuery), model);
    public static string Sql(IRunnable model, Type resultType) => SqlCore(typeof(RunnerQuery), model, resultType).Text.Trim();
}
