using System.Collections.Generic;
using System.Threading.Tasks;
using Jerrycurl.Razor.Projections;

namespace Jerrycurl.Razor.Test.Project.Queries.Misc;

// Hand-written equivalent of what the generator emits for a @query page.
public class EntryPointQuery : ProcPage<object, object>
{
    public EntryPointQuery(IProjection model, IProjection result)
        : base(model, result)
    {

    }

    public override void Render() => this.WriteLiteral("SELECT 1 AS `Item` UNION SELECT 2 AS `Item`");

    public static IList<int> Query() => QueryCore<int>(typeof(EntryPointQuery), null);
    public static Task<IList<int>> QueryAsync() => QueryCoreAsync<int>(typeof(EntryPointQuery), null);
    public static int Aggregate() => AggregateCore<int>(typeof(EntryPointQuery), null);
    public static IEnumerable<int> Enumerate() => EnumerateCore<int>(typeof(EntryPointQuery), null);
    public static IList<int> Query(IProcEngine engine) => QueryCore<int>(typeof(EntryPointQuery), null, engine: engine);
}
