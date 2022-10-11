using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jerrycurl.Cqs.Queries;
using Jerrycurl.Relations;
using Jerrycurl.Relations.Language;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Language
{
    public static class QueryExtensions
    {
        public static T Commit<T>(this QueryBuffer buffer)
            => (T)buffer.Commit();


        #region " Engine "
        public static IList<T> List<T>(this QueryEngine engine, Query query)
            => engine.List<T>(new[] { query });

        public static IList<T> List<T>(this QueryEngine engine, IEnumerable<Query> queries)
            => engine.Execute<IList<T>>(queries, QueryType.List);

        public static Task<IList<T>> ListAsync<T>(this QueryEngine engine, Query query, CancellationToken cancellationToken = default)
            => engine.ListAsync<T>(new[] { query }, cancellationToken);

        public static Task<IList<T>> ListAsync<T>(this QueryEngine engine, IEnumerable<Query> queries, CancellationToken cancellationToken = default)
            => engine.ExecuteAsync<IList<T>>(queries, QueryType.List, cancellationToken);

        public static T Aggregate<T>(this QueryEngine engine, IEnumerable<Query> queries)
            => engine.Execute<T>(queries, QueryType.Aggregate);

        public static T Aggregate<T>(this QueryEngine engine, Query query)
            => engine.Aggregate<T>(new[] { query });

        public static Task<T> AggregateAsync<T>(this QueryEngine engine, IEnumerable<Query> queries, CancellationToken cancellationToken = default)
            => engine.ExecuteAsync<T>(queries, QueryType.Aggregate, cancellationToken);

        public static Task<T> AggregateAsync<T>(this QueryEngine engine, Query query, CancellationToken cancellationToken = default)
            => engine.AggregateAsync<T>(new[] { query }, cancellationToken);

        #endregion

        #region " Insert "
        public static void InsertAll(this QueryBuffer buffer, IDataReader dataReader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            do
            {
                buffer.Insert(dataReader);
            }
            while (dataReader.NextResult());
        }

        public static QueryBuffer Insert(this QueryBuffer buffer, IRelation relation, params string[] targetHeader)
            => buffer.Insert(relation, (IEnumerable<string>)targetHeader);
        public static QueryBuffer Insert(this QueryBuffer buffer, IRelation relation, IEnumerable<IRelationMetadata> targetHeader)
            => buffer.Insert(relation, targetHeader?.Select(m => m?.Identity.Name));

        public static QueryBuffer Insert(this QueryBuffer buffer, IRelation relation, IEnumerable<string> targetHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(relation));

            using IDataReader dataReader = relation.GetDataReader(targetHeader);

            buffer.Insert(dataReader);

            return buffer;
        }

        public static QueryBuffer Insert(this QueryBuffer buffer, IRelation relation)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(relation));

            using IDataReader dataReader = relation.GetDataReader();

            buffer.Insert(dataReader);

            return buffer;
        }

        public static QueryBuffer Insert<TSource>(this QueryBuffer buffer, TSource source, params string[] sourceHeader)
            => buffer.Insert(source, (IEnumerable<string>)sourceHeader);

        public static QueryBuffer Insert<TSource>(this QueryBuffer buffer, TSource source, IEnumerable<string> sourceHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            ISchema schema = buffer.Store.GetSchema(typeof(TSource));

            buffer.Insert(source, schema.Select(sourceHeader));

            return buffer;
        }

        public static QueryBuffer Insert<TSource>(this QueryBuffer buffer, TSource source, RelationHeader sourceHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IRelation relation = new Relation(buffer.Store.From(source), sourceHeader);

            buffer.Insert(relation);

            return buffer;
        }

        public static QueryBuffer Insert<TSource>(this QueryBuffer buffer, TSource source, params (string Source, string Target)[] mappingHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (mappingHeader == null)
                throw new ArgumentNullException(nameof(mappingHeader));

            IEnumerable<string> sourceHeader = mappingHeader.Select(t => t.Source);
            IEnumerable<string> targetHeader = mappingHeader.Select(t => t.Target);

            buffer.Insert(source, sourceHeader, targetHeader);

            return buffer;
        }

        public static QueryBuffer Insert<TSource>(this QueryBuffer buffer, TSource source, IEnumerable<string> sourceHeader, IEnumerable<string> targetHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IRelation relation = buffer.Store.From(source).Select(sourceHeader);

            buffer.Insert(relation, targetHeader);

            return buffer;
        }
        #endregion

        #region " InsertAsync "
        public static async Task InsertAllAsync(this QueryBuffer buffer, DbDataReader dataReader, CancellationToken cancellationToken = default)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            do
            {
                await buffer.InsertAsync(dataReader, cancellationToken).ConfigureAwait(false);
            }
            while (await dataReader.NextResultAsync().ConfigureAwait(false));
        }

        public static Task<QueryBuffer> InsertAsync(this QueryBuffer buffer, IRelation relation, params string[] targetHeader)
            => buffer.InsertAsync(relation, (IEnumerable<string>)targetHeader);
        public static Task<QueryBuffer> InsertAsync(this QueryBuffer buffer, IRelation relation, IEnumerable<IRelationMetadata> targetHeader)
            => buffer.InsertAsync(relation, targetHeader?.Select(m => m?.Identity.Name));

        public static async Task<QueryBuffer> InsertAsync(this QueryBuffer buffer, IRelation relation, IEnumerable<string> targetHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(relation));

            using DbDataReader dataReader = relation.GetDataReader(targetHeader);

            await buffer.InsertAsync(dataReader, default);

            return buffer;
        }

        public static async Task<QueryBuffer> InsertAsync(this QueryBuffer buffer, IRelation relation)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (relation == null)
                throw new ArgumentNullException(nameof(relation));

            using DbDataReader dataReader = relation.GetDataReader();

            await buffer.InsertAsync(dataReader, default);

            return buffer;
        }

        public static Task<QueryBuffer> InsertAsync<TSource>(this QueryBuffer buffer, TSource source, params string[] sourceHeader)
            => buffer.InsertAsync(source, (IEnumerable<string>)sourceHeader);

        public static async Task<QueryBuffer> InsertAsync<TSource>(this QueryBuffer buffer, TSource source, IEnumerable<string> sourceHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            ISchema schema = buffer.Store.GetSchema(typeof(TSource));

            await buffer.InsertAsync(source, schema.Select(sourceHeader));

            return buffer;
        }

        public static async Task<QueryBuffer> InsertAsync<TSource>(this QueryBuffer buffer, TSource source, RelationHeader sourceHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IRelation relation = new Relation(buffer.Store.From(source), sourceHeader);

            await buffer.InsertAsync(relation);

            return buffer;
        }

        public static async Task<QueryBuffer> InsertAsync<TSource>(this QueryBuffer buffer, TSource source, params (string Source, string Target)[] mappingHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (mappingHeader == null)
                throw new ArgumentNullException(nameof(mappingHeader));

            IEnumerable<string> sourceHeader = mappingHeader.Select(t => t.Source);
            IEnumerable<string> targetHeader = mappingHeader.Select(t => t.Target);

            await buffer.InsertAsync(source, sourceHeader, targetHeader);

            return buffer;
        }

        public static async Task<QueryBuffer> InsertAsync<TSource>(this QueryBuffer buffer, TSource source, IEnumerable<string> sourceHeader, IEnumerable<string> targetHeader)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            IRelation relation = buffer.Store.From(source).Select(sourceHeader);

            await buffer.InsertAsync(relation, targetHeader);

            return buffer;
        }
        #endregion
    }
}
