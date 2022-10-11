using Jerrycurl.Cqs.Queries.Internal.Caching;
using Jerrycurl.Cqs.Queries.Internal.Compilation;
using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Jerrycurl.Cqs.Queries
{
    public sealed class QueryReader
    {
        public ISchemaStore Store { get; }

        private readonly IDataReader syncReader;
        private readonly DbDataReader asyncReader;

        public QueryReader(ISchemaStore store, IDataReader dataReader)
        {
            this.Store = store ?? throw new ArgumentNullException(nameof(store));
            this.syncReader = dataReader ?? throw new ArgumentNullException(nameof(dataReader));
            this.asyncReader = dataReader as DbDataReader;
        }

        public async IAsyncEnumerable<T> ReadAsync<T>([EnumeratorCancellation]CancellationToken cancellationToken = default)
        {
            if (this.asyncReader == null)
                throw new QueryException("Async not available. To use async operations, instantiate with a DbDataReader instance.");

            ISchema schema = this.Store.GetSchema(typeof(IList<T>));
            EnumerateFactory<T> reader = QueryCache.GetEnumerateFactory<T>(schema, this.asyncReader);

            while (await this.asyncReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                yield return reader(this.asyncReader);
        }

        public IEnumerable<T> Read<T>()
        {
            ISchema schema = this.Store.GetSchema(typeof(IList<T>));
            EnumerateFactory<T> reader = QueryCache.GetEnumerateFactory<T>(schema, this.syncReader);

            while (this.syncReader.Read())
                yield return reader(this.syncReader);
        }
    }
}