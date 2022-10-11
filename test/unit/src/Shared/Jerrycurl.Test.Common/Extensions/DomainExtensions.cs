using System;
using System.Data;
using System.Data.Common;
using Jerrycurl.Mvc;
using Jerrycurl.Test.Profiling;

namespace Jerrycurl.Test.Extensions
{
    public static class DomainExtensions
    {
        public static DomainOptions UseProfiling(this DomainOptions options)
        {
            Func<IDbConnection> innerConnection = options.ConnectionFactory;

            options.ConnectionFactory = () => new ProfilingConnection((DbConnection)innerConnection());

            return options;
        }
    }
}
