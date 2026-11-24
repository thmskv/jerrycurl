using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Jerrycurl.Cqs.Filters;

public class TransactionFilter : IFilter
{
    private readonly Func<Handler> factory;
    private readonly Func<AsyncHandler> asyncFactory;

    public TransactionFilter()
    {
        this.factory = () => new Handler(c => c.BeginTransaction());
        this.asyncFactory = () => new AsyncHandler(c => c.BeginTransaction());
    }

    public TransactionFilter(IsolationLevel isolationLevel)
    {
        this.factory = () => new Handler(c => c.BeginTransaction(isolationLevel));
        this.asyncFactory = () => new AsyncHandler(c => c.BeginTransaction(isolationLevel));
    }

    public IFilterHandler GetHandler(IDbConnection connection) => this.factory();
    public IFilterAsyncHandler GetAsyncHandler(IDbConnection connection) => this.asyncFactory();

    private class Handler : FilterHandler
    {
        private readonly Func<IDbConnection, IDbTransaction> factory;
        private IDbTransaction transaction;
        private bool handled = false;
        private int pendingCommands = 0;

        public Handler(Func<IDbConnection, IDbTransaction> factory)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public override void OnConnectionOpened(FilterContext context)
        {
            this.transaction = this.factory(context.Connection);
        }

        public override void OnCommandCreated(FilterContext context)
        {
            context.Command.Transaction = this.transaction;
            this.pendingCommands++;
        }

        public override void OnCommandExecuted(FilterContext context)
        {
            this.pendingCommands--;
        }

        public override void OnException(FilterContext context)
        {
            if (!this.handled)
            {
                try
                {
                    this.transaction.Rollback();
                }
                catch { }
            }

            this.handled = true;
        }

        public override void OnConnectionClosing(FilterContext context)
        {
            if (!this.handled)
            {
                // A command whose results were not fully consumed means execution was aborted
                // (e.g. an exception while reading results), so the transaction must not be committed.
                if (this.pendingCommands > 0)
                    this.transaction.Rollback();
                else
                    this.transaction.Commit();
            }

            this.handled = true;
        }

        public override void Dispose()
        {
            this.transaction?.Dispose();
            this.transaction = null;
        }
    }

    private class AsyncHandler : FilterHandler
    {
        private readonly Func<IDbConnection, IDbTransaction> factory;
        private DbTransaction transaction;
        private bool handled = false;
        private int pendingCommands = 0;

        public AsyncHandler(Func<IDbConnection, IDbTransaction> factory)
        {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public override Task OnConnectionOpenedAsync(FilterContext context)
        {
            this.transaction = this.factory(context.Connection) as DbTransaction;

            return Task.CompletedTask;
        }

        public override Task OnCommandCreatedAsync(FilterContext context)
        {
            context.Command.Transaction = this.transaction;
            this.pendingCommands++;

            return Task.CompletedTask;
        }

        public override Task OnCommandExecutedAsync(FilterContext context)
        {
            this.pendingCommands--;

            return Task.CompletedTask;
        }

        public override async Task OnExceptionAsync(FilterContext context)
        {
            if (!this.handled)
            {
                try
                {
                    await this.transaction.RollbackAsync().ConfigureAwait(false);
                }
                catch { }
            }

            this.handled = true;
        }

        public override async Task OnConnectionClosingAsync(FilterContext context)
        {
            if (!this.handled)
            {
                // See Handler.OnConnectionClosing.
                if (this.pendingCommands > 0)
                    await this.transaction.RollbackAsync().ConfigureAwait(false);
                else
                    await this.transaction.CommitAsync().ConfigureAwait(false);
            }

            this.handled = true;
        }

        public override async ValueTask DisposeAsync()
        {
            if (this.transaction != null)
                await this.transaction.DisposeAsync().ConfigureAwait(false);

            this.transaction = null;
        }
    }
}