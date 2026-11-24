using Jerrycurl.Razor.Test.Project.Queries.Misc;
using Shouldly;

namespace Jerrycurl.Razor.Test;

public class TemplateTests
{
    public void Test_Procedure_Template()
    {
        TemplatedQuery.Query<int>().ShouldBe([1, 2, 3]);
    }

    public void Test_Partial_Template()
    {
        PartialedQuery.Query<int>().ShouldBe([1, 2, 3]);
    }

    public void Test_Template_Cycle_Throws()
    {
        Should.Throw<ProcExecutionException>(() => new ProcEngine(null).Proc(new ProcEngine(null).Describe(typeof(CycleA)), new ProcArgs(typeof(object), typeof(object))));
    }
}
