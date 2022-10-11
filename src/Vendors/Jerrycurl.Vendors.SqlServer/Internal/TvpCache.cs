using Jerrycurl.Relations;
using System;
using System.Collections.Concurrent;
#if NET20_BASE
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif


namespace Jerrycurl.Vendors.SqlServer.Internal
{
    internal static class TvpCache
    {
        public static ConcurrentDictionary<IRelationHeader, Action<SqlParameter, IRelation>> Binders { get; } = new ConcurrentDictionary<IRelationHeader, Action<SqlParameter, IRelation>>();
    }
}
