using Jerrycurl.Mvc.Test.Project.Accessors;
using Shouldly;

namespace Jerrycurl.Mvc.Test;

public class TemplateTests
{
    public void Test_Procedure_Template()
    {
        var misc = new MiscAccessor();
        var result = misc.TemplatedQuery();

        result.ShouldBe([1, 2, 3]);
    }

    public void Test_Partial_Template()
    {
        var misc = new MiscAccessor();
        var result = misc.PartialedQuery();

        result.ShouldBe([1, 2, 3]);
    }
}
