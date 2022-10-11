using System;
using System.Collections.Generic;
using System.Data;

namespace Jerrycurl.Cqs.Sessions
{
    public interface ISyncSession : IDisposable
    {
        IEnumerable<IDataReader> Execute(IBatch batch);
    }
}
