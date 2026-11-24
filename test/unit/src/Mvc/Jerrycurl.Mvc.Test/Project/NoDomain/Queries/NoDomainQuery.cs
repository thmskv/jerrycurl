using Jerrycurl.Mvc.Projections;

namespace Jerrycurl.Mvc.Test.Project2.NoDomain.Queries;

public class NoDomainQuery : ProcPage<object, object>
{
    public NoDomainQuery(IProjection model, IProjection result)
        : base(model, result)
    {

    }
}
