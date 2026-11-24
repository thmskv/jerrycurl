using Jerrycurl.Mvc.Metadata.Annotations;
using Jerrycurl.Mvc.Projections;

namespace Jerrycurl.Mvc.Test.Project.Queries.Shared;

[Template("TemplateQuery1.cssql")]
public class TemplateQuery2_cssql : ProcPage<object, object>
{
    public TemplateQuery2_cssql(IProjection model, IProjection result)
        : base(model, result)
    {

    }

    public override void Render()
    {
        this.WriteLiteral("SELECT 2 AS `Item` UNION\r\n");
        this.Write(this.RenderBody());
    }
}
