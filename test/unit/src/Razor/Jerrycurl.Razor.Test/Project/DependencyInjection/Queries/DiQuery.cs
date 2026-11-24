using Jerrycurl.Razor.Metadata.Annotations;
using Jerrycurl.Razor.Projections;
using Jerrycurl.Razor.Test.Project.DependencyInjection.Services;

namespace Jerrycurl.Razor.Test.Project.DependencyInjection.Queries;

public class DiQuery : ProcPage<object, object>
{
    [Inject]
    public MyService ms { get; set; }

    [Inject]
    public IProjection<int> p1 { get; set; }

    public DiQuery(IProjection model, IProjection result)
        : base(model, result)
    {

    }

    public override void Render()
    {
        this.WriteLiteral(this.ms.SomeValue);

        if (this.p1 != null)
            this.WriteLiteral("+PROJ");
    }
}
