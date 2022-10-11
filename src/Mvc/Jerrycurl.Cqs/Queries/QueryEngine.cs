using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Jerrycurl.Collections;
using System.Data.Common;
using System.Threading;
using System.Runtime.CompilerServices;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Queries
{
    public class QueryEngine
    {
        public QueryOptions Options { get; }

        public QueryEngine(QueryOptions options)
        {
            this.Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        #region " Execute "
        public TResult Execute<TResult>(Query query, QueryType queryType)
            => this.Execute<TResult>(new[] { query }, queryType);

        public TResult Execute<TResult>(IEnumerable<Query> queries, QueryType queryType)
        {
            if (queries == null)
                throw new ArgumentNullException(nameof(queries));

            if (this.Options.Store == null)
                throw new InvalidOperationException("No schema store found.");

            ISchema schema = this.Options.Store.GetSchema(typeof(TResult));
            QueryBuffer buffer = new QueryBuffer(schema, queryType);

            using ISyncSession connection = this.Options.GetSyncSession();

            foreach (IBatch batch in this.FilterBatches(queries))
            {
                foreach (IDataReader dataReader in connection.Execute(batch))
                    buffer.Insert(dataReader);
            }

            return (TResult)buffer.Commit();
        }

        public Task<TResult> ExecuteAsync<TResult>(Query query, QueryType queryType, CancellationToken cancellationToken = default)
            => this.ExecuteAsync<TResult>(new[] { query }, queryType, cancellationToken);

        public async Task<TResult> ExecuteAsync<TResult>(IEnumerable<Query> queries, QueryType queryType, CancellationToken cancellationToken = default)
        {
            if (queries == null)
                throw new ArgumentNullException(nameof(queries));

            if (this.Options.Store == null)
                throw new InvalidOperationException("No schema builder found.");

            ISchema schema = this.Options.Store.GetSchema(typeof(TResult));
            QueryBuffer buffer = new QueryBuffer(schema, queryType);

            await using IAsyncSession connection = this.Options.GetAsyncSession();

            foreach (IBatch batch in this.FilterBatches(queries))
            {
                await foreach (DbDataReader dataReader in connection.ExecuteAsync(batch, cancellationToken).ConfigureAwait(false))
                    await buffer.InsertAsync(dataReader, cancellationToken).ConfigureAwait(false);
            }

            return (TResult)buffer.Commit();
        }
        #endregion

        #region " Enumerate "

        public IAsyncEnumerable<QueryReader> EnumerateAsync(Query query, CancellationToken cancellationToken = default) => this.EnumerateAsync(query, cancellationToken);
        public async IAsyncEnumerable<QueryReader> EnumerateAsync(IEnumerable<Query> queries, [EnumeratorCancellation]CancellationToken cancellationToken = default)
        {
            if (queries == null)
                throw new ArgumentNullException(nameof(queries));

            if (this.Options.Store == null)
                throw new InvalidOperationException("No schema builder found.");

            await using IAsyncSession connection = this.Options.GetAsyncSession();

            foreach (IBatch batch in this.FilterBatches(queries))
            {
                await foreach (DbDataReader dataReader in connection.ExecuteAsync(batch, cancellationToken).ConfigureAwait(false))
                    yield return new QueryReader(this.Options.Store, dataReader);
            }
        }

        public IAsyncEnumerable<TItem> EnumerateAsync<TItem>(Query query, CancellationToken cancellationToken = default) => this.EnumerateAsync<TItem>(new[] { query }, cancellationToken);
        public async IAsyncEnumerable<TItem> EnumerateAsync<TItem>(IEnumerable<Query> queries, [EnumeratorCancellation]CancellationToken cancellationToken = default)
        {
            await foreach (QueryReader queryReader in this.EnumerateAsync(queries, cancellationToken).ConfigureAwait(false))
            {
                await foreach (TItem item in queryReader.ReadAsync<TItem>(cancellationToken).ConfigureAwait(false))
                    yield return item;
            }
        }

        public IEnumerable<TItem> Enumerate<TItem>(Query query) => this.Enumerate<TItem>(new[] { query });
        public IEnumerable<TItem> Enumerate<TItem>(IEnumerable<Query> queries) => this.Enumerate(queries).SelectMany(r => r.Read<TItem>());

        public IEnumerable<QueryReader> Enumerate(Query query) => this.Enumerate(new[] { query });
        public IEnumerable<QueryReader> Enumerate(IEnumerable<Query> queries)
        {
            if (queries == null)
                throw new ArgumentNullException(nameof(queries));

            if (this.Options.Store == null)
                throw new InvalidOperationException("No schema builder found.");

            using ISyncSession connection = this.Options.GetSyncSession();

            foreach (IBatch batch in this.FilterBatches(queries))
            {
                foreach (IDataReader reader in connection.Execute(batch))
                    yield return new QueryReader(this.Options.Store, reader);
            }
        }

        #endregion

        private IEnumerable<IBatch> FilterBatches(IEnumerable<Query> queries)
            => queries.NotNull().Where(d => !string.IsNullOrWhiteSpace(d.QueryText));
    }
}
