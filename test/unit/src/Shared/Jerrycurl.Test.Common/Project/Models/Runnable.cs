﻿using Jerrycurl.Mvc;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Mvc.Sql;
using System;
using System.Collections.Generic;

namespace Jerrycurl.Test.Project.Models
{
    public class Runnable<TModel, TResult> : IRunnable
    {
        private readonly List<ActionItem> actions = new List<ActionItem>();
        public TModel Model { get; }
        public string Separator { get; }

        public Runnable(TModel model = default, string separator = null)
        {
            this.Model = model;
            this.Separator = separator;
        }

        private void R(Func<IProjection, ISqlWritable> func) => this.actions.Add(new ActionItem() { Result = func });
        private void M(Func<IProjection, ISqlWritable> func) => this.actions.Add(new ActionItem() { Model = func });

        public void M(Func<IProjection<TModel>, ISqlWritable> func) => this.M(p => func(p.Cast<Runnable<TModel, TResult>>().Val(m => m.Model)));
        public void R(Func<IProjection<TResult>, ISqlWritable> func) => this.R(p => func(p.Cast<TResult>()));

        public void M(ISqlWritable writable) => this.M(p => writable);
        public void R(ISqlWritable writable) => this.R(p => writable);

        public void Sql(string sql) => this.actions.Add(new ActionItem() { Sql = sql });

        public void Execute(ISqlPage page)
        {
            var proc = (ProcPage<IRunnable, object>)page;

            foreach (ActionItem a in this.actions)
            {
                if (a.Sql != null)
                    proc.WriteLiteral(a.Sql);

                if (a.Result != null)
                    proc.Write(a.Result(WithOptions(proc.R)));

                if (a.Model != null)
                    proc.Write(a.Model(WithOptions(proc.M)));
            }

            IProjection WithOptions(IProjection projection)
            {
                if (this.Separator == null)
                    return projection;

                var newOptions = new ProjectionOptions(projection.Options) { Separator = this.Separator };

                return projection.With(options: newOptions);
            }
        }

        private class ActionItem
        {
            public string Sql { get; set; }
            public Func<IProjection, ISqlWritable> Result { get; set; }
            public Func<IProjection, ISqlWritable> Model { get; set; }
        }
    }

    public class Runnable : Runnable<object, object>
    {
        public static Runnable<TModel, object> Command<TModel>(TModel model, string separator = null)
            => new Runnable<TModel, object>(model);

        public static Runnable<object, TResult> Query<TResult>(object model = null, string separator = null)
            => new Runnable<object, TResult>(model);
    }

    public interface IRunnable
    {
        void Execute(ISqlPage page);
    }
}
