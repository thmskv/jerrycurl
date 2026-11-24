using Jerrycurl.Razor.Metadata.Annotations;
using Jerrycurl.Razor.Projections;

namespace Jerrycurl.Razor.Test.Project.Queries.Misc;

public class ProjectedQuery : ProcPage<object, object>
{
    [Inject]
    public IProjection<int> p1 { get; set; }

    public ProjectedQuery(IProjection model, IProjection result)
        : base(model, result)
    {

    }

    public override void Render()
    {
        if (this.p1 != null)
            this.WriteLiteral("PROJEXISTS");
    }
}
