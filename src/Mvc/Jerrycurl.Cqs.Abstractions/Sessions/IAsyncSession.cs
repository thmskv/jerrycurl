using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

namespace Jerrycurl.Cqs.Sessions
{
    public partial interface IAsyncSession : IAsyncDisposable
    {
        IAsyncEnumerable<DbDataReader> ExecuteAsync(IBatch batch, CancellationToken cancellationToken = default);
    }
}
