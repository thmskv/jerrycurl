using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Cqs.Queries;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Mvc.Projections;
using Jerrycurl.Mvc.Sql;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Language;

namespace Jerrycurl.Mvc;

public class ProcPage<TModel, TResult> : ISqlPage
{
    public IProjection<TModel> M { get; }
    public IProjection<TResult> R { get; }
    public TModel Model { get; }

    public IProcContext Context { get; }
    public IProcRenderer Render => this.Context.Renderer;
    
    public ProcPage(IProjection model, IProjection result)
    {
        this.M = model?.Cast<TModel>() ?? throw new ArgumentNullException(nameof(model));
        this.R = result?.Cast<TResult>() ?? throw new ArgumentNullException(nameof(result));
        this.Context = model.Context;
        this.Model = (TModel)(this.M.Attr().Data?.Source.Snapshot ?? default);
    }

    public virtual void Execute() { }
    public virtual void Throw(Exception ex) { }

    public void Write<T>(T value)
    {
        if (value is ISqlWritable w)
            w.WriteTo(this.Context.Execution.Buffer);
        else
        {
            IField model = this.Context.Domain.Schemas.From(value);

            ProjectionIdentity identity = new ProjectionIdentity(model);
            IProjectionMetadata metadata = model.Identity.Schema.Require<IProjectionMetadata>();
            Projection projection = new Projection(identity, this.Context, metadata);

            this.Write(projection.Par());
        }
    }

    public void Write(object o) => this.Write<object>(o);
    public void WriteLiteral(string s) => this.Context.Execution.Buffer.Append(s);

#region " Page shenanigans "
    private static IProcResult ExecuteAndGetResult(Type pageType, object model, ProcArgs args)
    {
        IProcLocator locator = new ProcLocator();
        IProcEngine engine = new ProcEngine(null);

        PageDescriptor descriptor = locator.Create(pageType);
        ProcFactory factory = engine.Proc(descriptor, args);

        return factory(model);
    }

    private static SqlOptions GetDefaultOptions(IDomainOptions domain)
    {
        return new SqlOptions()
        {
            Filters = [.. domain.Sql.Filters],
            MaxParameters = domain.Sql.MaxParameters,
            MaxSql = domain.Sql.MaxSql,
        };
    }

    public static async Task<IList<TFinal>> Query<TFinal>(Type pageType, TModel model, Action<SqlOptions> configure = null, CancellationToken cancellationToken = default)
        where TFinal : TResult
    {
        ProcArgs args = new ProcArgs(typeof(TModel), typeof(TFinal));
        IProcResult result = ExecuteAndGetResult(pageType, model, args);

        SqlOptions options = GetDefaultOptions(result.Domain);
        configure?.Invoke(options);

        ISqlSerializer<Query> serializer = result.Buffer as ISqlSerializer<Query>;
        IEnumerable<Query> queries = serializer?.Serialize(options);

        QueryOptions queryOptions = new QueryOptions()
        {
            ConnectionFactory = result.Domain.ConnectionFactory,
            Store = result.Domain.Schemas,
            Filters = options.Filters,
        };

        QueryEngine engine = new QueryEngine(queryOptions);

        try
        {
            return await engine.ExecuteAsync<IList<TFinal>>(queries, QueryType.List, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result.Page.Throw(ex);

            throw;
        }
    }
#endregion
}