using System;
using System.Collections.Generic;
using System.Data;
using Jerrycurl.Cqs.Filters;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Queries
{
    public class QueryOptions : ISessionOptions
    {
        public Func<IDbConnection> ConnectionFactory { get; set; }
        public ICollection<IFilter> Filters { get; set; } = new List<IFilter>();
        public ISchemaStore Store { get; set; }

        public virtual IAsyncSession GetAsyncSession() => new AsyncSession(this.ConnectionFactory, this.Filters);
        public virtual ISyncSession GetSyncSession() => new SyncSession(this.ConnectionFactory, this.Filters);
    }
}
