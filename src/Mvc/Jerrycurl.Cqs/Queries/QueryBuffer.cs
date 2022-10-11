using Jerrycurl.Cqs.Queries.Internal;
using Jerrycurl.Cqs.Queries.Internal.Caching;
using Jerrycurl.Cqs.Queries.Internal.Compilation;
using Jerrycurl.Relations.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Jerrycurl.Cqs.Queries
{
    public sealed class QueryBuffer : IQueryBuffer
    {
        public ISchemaStore Store => this.Schema.Store;
        public ISchema Schema { get; }
        public QueryType Type { get; }

        ElasticArray IQueryBuffer.AggregateData => this.aggregateData;
        ElasticArray IQueryBuffer.ListData => this.listData;
        List<AggregateAttribute> IQueryBuffer.AggregateHeader => this.aggregateHeader;

        private ElasticArray aggregateData;
        private ElasticArray listData;
        private List<AggregateAttribute> aggregateHeader;

        public QueryBuffer(ISchema schema, QueryType type)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
            this.Type = type;

            this.Flush();
        }

        private void Flush()
        {
            this.listData = new ElasticArray();

            if (this.Type == QueryType.Aggregate)
            {
                this.aggregateData = new ElasticArray();
                this.aggregateHeader = new List<AggregateAttribute>();
            }
            else if (this.Type != QueryType.List)
                throw QueryException.InvalidQueryType(this.Type);
        }

        public object Commit()
        {
            try
            {
                return this.Type switch
                {
                    QueryType.List => this.CommitList(),
                    QueryType.Aggregate => this.CommitAggregate(),
                    _ => throw new InvalidOperationException(),
                };
            }
            finally
            {
                this.Flush();
            }
        }

        public void Insert(IDataReader dataReader)
        {
            ListFactory factory = QueryCache.GetListFactory(this.Schema, this.Type, dataReader);

            factory.WriteAll(this, dataReader);
        }

        public async Task InsertAsync(DbDataReader dataReader, CancellationToken cancellationToken)
        {
            ListFactory factory = QueryCache.GetListFactory(this.Schema, this.Type, dataReader);

            factory.Initialize(this);

            while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                factory.WriteOne(this, dataReader);
        }


        private object CommitAggregate()
        {
            AggregateFactory factory = QueryCache.GetAggregateFactory(this.Schema, this.aggregateHeader);

            return factory(this);
        }
        private object CommitList() => this.listData[0];
    }
}
