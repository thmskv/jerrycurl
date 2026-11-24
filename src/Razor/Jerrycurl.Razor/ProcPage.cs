using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Queries;
using Jerrycurl.Razor.Metadata;
using Jerrycurl.Razor.Projections;
using Jerrycurl.Razor.Sql;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Language;

namespace Jerrycurl.Razor;

public class ProcPage<TModel, TResult> : ISqlPage
{
    public IProjection<TModel> M { get; }
    public IProjection<TResult> R { get; }
    public TModel Model { get; }

    public IProcContext Context { get; }
    public IProcRenderer Renderer => this.Context.Renderer;

    public ProcPage(IProjection model, IProjection result)
    {
        this.M = model?.Cast<TModel>() ?? throw new ArgumentNullException(nameof(model));
        this.R = result?.Cast<TResult>() ?? throw new ArgumentNullException(nameof(result));
        this.Context = model.Context;
        this.Model = (TModel)(this.M.Attr().Data?.Source.Snapshot ?? default);
    }

    public virtual void Render() { }
    public virtual void Throw(Exception ex) { }

    /// <summary>
    /// Renders the body of the page using this page as its template. Only valid in template pages.
    /// </summary>
    public ISqlContent RenderBody() => this.Renderer.Body();

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

    #region " Entry points "
    // Called by the static Query/Execute/... methods generated for @query and @command pages.

    private static IProcResult Run(Type pageType, object model, Type resultType, IProcEngine engine)
    {
        ArgumentNullException.ThrowIfNull(pageType);

        engine ??= ProcEngine.Default;

        PageDescriptor descriptor = engine.Describe(pageType);
        ProcArgs args = new ProcArgs(model?.GetType() ?? typeof(TModel), resultType);
        ProcFactory factory = engine.Proc(descriptor, args);

        return factory(model);
    }

    private static SqlOptions GetOptions(IDomainOptions domain, Action<SqlOptions> configure)
    {
        SqlOptions options = new SqlOptions()
        {
            Filters = [.. domain.Sql.Filters],
            MaxParameters = domain.Sql.MaxParameters,
            MaxSql = domain.Sql.MaxSql,
        };

        configure?.Invoke(options);

        return options;
    }

    private static (QueryEngine Engine, IEnumerable<Query> Queries, IProcResult Result) PrepareQuery<TItem>(Type pageType, object model, Action<SqlOptions> configure, IProcEngine engine)
    {
        IProcResult result = Run(pageType, model, typeof(IList<TItem>), engine);
        SqlOptions options = GetOptions(result.Domain, configure);

        ISqlSerializer<Query> serializer = result.Buffer as ISqlSerializer<Query>;
        IEnumerable<Query> queries = serializer?.Serialize(options);

        QueryOptions queryOptions = new QueryOptions()
        {
            ConnectionFactory = result.Domain.ConnectionFactory,
            Store = result.Domain.Schemas,
            Filters = options.Filters,
        };

        return (new QueryEngine(queryOptions), queries, result);
    }

    private static (CommandEngine Engine, IEnumerable<Command> Commands, IProcResult Result) PrepareCommand(Type pageType, object model, Action<SqlOptions> configure, IProcEngine engine)
    {
        IProcResult result = Run(pageType, model, typeof(object), engine);
        SqlOptions options = GetOptions(result.Domain, configure);

        ISqlSerializer<Command> serializer = result.Buffer as ISqlSerializer<Command>;
        IEnumerable<Command> commands = serializer.Serialize(options);

        CommandOptions commandOptions = new CommandOptions()
        {
            ConnectionFactory = result.Domain.ConnectionFactory,
            Filters = options.Filters,
        };

        return (new CommandEngine(commandOptions), commands, result);
    }

    /// <summary>
    /// Renders <paramref name="pageType"/> with <paramref name="model"/> and returns its SQL without executing it.
    /// </summary>
    protected static ISqlContent SqlCore(Type pageType, object model, Type resultType = null, IProcEngine engine = null)
        => Run(pageType, model, resultType ?? typeof(IList<TResult>), engine).Buffer.ReadToEnd();

    protected static IList<TItem> QueryCore<TItem>(Type pageType, object model, Action<SqlOptions> configure = null, IProcEngine engine = null)
    {
        var (queryEngine, queries, result) = PrepareQuery<TItem>(pageType, model, configure, engine);

        try
        {
            return queryEngine.Execute<IList<TItem>>(queries, QueryType.List);
        }
        catch (Exception ex)
        {
            result.Page.Throw(ex);

            throw;
        }
    }

    protected static async Task<IList<TItem>> QueryCoreAsync<TItem>(Type pageType, object model, Action<SqlOptions> configure = null, IProcEngine engine = null, CancellationToken cancellationToken = default)
    {
        var (queryEngine, queries, result) = PrepareQuery<TItem>(pageType, model, configure, engine);

        try
        {
            return await queryEngine.ExecuteAsync<IList<TItem>>(queries, QueryType.List, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result.Page.Throw(ex);

            throw;
        }
    }

    protected static TItem AggregateCore<TItem>(Type pageType, object model, Action<SqlOptions> configure = null, IProcEngine engine = null)
    {
        var (queryEngine, queries, result) = PrepareQuery<TItem>(pageType, model, configure, engine);

        try
        {
            IList<TItem> list = queryEngine.Execute<IList<TItem>>(queries, QueryType.Aggregate);

            return list == null || list.Count == 0 ? default : list[0];
        }
        catch (Exception ex)
        {
            result.Page.Throw(ex);

            throw;
        }
    }

    protected static async Task<TItem> AggregateCoreAsync<TItem>(Type pageType, object model, Action<SqlOptions> configure = null, IProcEngine engine = null, CancellationToken cancellationToken = default)
    {
        var (queryEngine, queries, result) = PrepareQuery<TItem>(pageType, model, configure, engine);

        try
        {
            IList<TItem> list = await queryEngine.ExecuteAsync<IList<TItem>>(queries, QueryType.Aggregate, cancellationToken).ConfigureAwait(false);

            return list == null || list.Count == 0 ? default : list[0];
        }
        catch (Exception ex)
        {
            result.Page.Throw(ex);

            throw;
        }
    }

    protected static IEnumerable<TItem> EnumerateCore<TItem>(Type pageType, object model, Action<SqlOptions> configure = null, IProcEngine engine = null)
    {
        var (queryEngine, queries, _) = PrepareQuery<TItem>(pageType, model, configure, engine);

        return queryEngine.Enumerate<TItem>(queries);
    }

    protected static IAsyncEnumerable<TItem> EnumerateCoreAsync<TItem>(Type pageType, object model, Action<SqlOptions> configure = null, IProcEngine engine = null, CancellationToken cancellationToken = default)
    {
        var (queryEngine, queries, _) = PrepareQuery<TItem>(pageType, model, configure, engine);

        return queryEngine.EnumerateAsync<TItem>(queries, cancellationToken);
    }

    protected static void ExecuteCore(Type pageType, object model, Action<SqlOptions> configure = null, IProcEngine engine = null)
    {
        var (commandEngine, commands, result) = PrepareCommand(pageType, model, configure, engine);

        try
        {
            commandEngine.Execute(commands);
        }
        catch (Exception ex)
        {
            result.Page.Throw(ex);

            throw;
        }
    }

    protected static async Task ExecuteCoreAsync(Type pageType, object model, Action<SqlOptions> configure = null, IProcEngine engine = null, CancellationToken cancellationToken = default)
    {
        var (commandEngine, commands, result) = PrepareCommand(pageType, model, configure, engine);

        try
        {
            await commandEngine.ExecuteAsync(commands, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result.Page.Throw(ex);

            throw;
        }
    }
    #endregion
}
