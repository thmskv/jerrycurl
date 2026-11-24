using System;
using System.Collections.Generic;
using System.Data;

namespace Jerrycurl.Cqs.Sessions;

public interface ISyncSession : IDisposable
{
    public IEnumerable<IDataReader> Execute(IBatch batch);
}
