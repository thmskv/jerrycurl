using System;
using System.Data;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Razor;

public interface IDomainOptions
{
    public Func<IDbConnection> ConnectionFactory { get; }
    public IDialect Dialect { get; }
    public ISchemaStore Schemas { get; }
    public IProcEngine Engine { get; }
    public IProcServices Services { get; }
    public ISqlOptions Sql { get; }
}
