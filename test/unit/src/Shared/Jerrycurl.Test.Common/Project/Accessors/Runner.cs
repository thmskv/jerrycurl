using System;
using System.Collections.Generic;
using Jerrycurl.Mvc;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Test.Project.Models;

namespace Jerrycurl.Test.Project.Accessors
{
    public class Runner : Accessor
    {
        private TResult AggregateInternal<TModel, TResult>(Runnable<TModel, TResult> runner) => this.Aggregate<TResult>(runner, queryName: "Query");
        private IList<TResult> QueryInternal<TModel, TResult>(Runnable<TModel, TResult> runner) => this.Query<TResult>(runner, queryName: "Query");
        private void CommandInternal<TModel, TResult>(Runnable<TModel, TResult> runner) => this.Execute(runner, commandName: "Command");

        public static TResult Aggregate<TModel, TResult>(Runnable<TModel, TResult> runner) => new Runner().AggregateInternal(runner);
        public static IList<TResult> Query<TModel, TResult>(Runnable<TModel, TResult> runner) => new Runner().QueryInternal(runner);
        public static void Command<TModel, TResult>(Runnable<TModel, TResult> runner) => new Runner().CommandInternal(runner);

        public string Sql<TModel, TResult>(Runnable<TModel, TResult> model)
        {
            IProcLocator locator = this.Context?.Locator ?? new ProcLocator();
            IProcEngine engine = this.Context?.Engine ?? new ProcEngine(null);

            PageDescriptor descriptor = locator.FindPage("Query", this.GetType());
            ProcArgs args = new ProcArgs(typeof(Runnable<TModel, TResult>), typeof(List<TResult>));
            ProcFactory factory = engine.Proc(descriptor, args);

            return factory(model).Buffer.ReadToEnd().Text.Trim();
        }

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
}
