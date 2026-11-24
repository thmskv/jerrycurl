using Jerrycurl.Razor.Projections;

namespace Jerrycurl.Razor.Test.Project2.NoDomain.Queries;

public class NoDomainQuery : ProcPage<object, object>
{
    public NoDomainQuery(IProjection model, IProjection result)
        : base(model, result)
    {

    }
}
