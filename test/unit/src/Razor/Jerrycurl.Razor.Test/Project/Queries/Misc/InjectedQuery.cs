using Jerrycurl.Razor.Metadata.Annotations;
using Jerrycurl.Razor.Projections;

namespace Jerrycurl.Razor.Test.Project.Queries.Misc;

public class InjectedQuery : ProcPage<object, object>
{
    [Inject]
    public DependencyInjection.Services.MyService ms { get; set; }

    public InjectedQuery(IProjection model, IProjection result)
        : base(model, result)
    {

    }

    public override void Render()
    {

    }
}
