using Jerrycurl.Mvc.Test.Project.Commands.Batching;
using Jerrycurl.Mvc.Test.Project2.NoDomain.Queries;
using Shouldly;

namespace Jerrycurl.Mvc.Test;

public class EngineTests
{
    private readonly ProcEngine engine = new ProcEngine(null);

    public void Test_Page_CanDescribe_NoDomain()
    {
        var descriptor = this.engine.Describe(typeof(NoDomainQuery));

        descriptor.ShouldNotBeNull();
        descriptor.DomainType.ShouldBeNull();

        var factory = Should.NotThrow(() => this.engine.Page(descriptor.PageType));

        factory.ShouldNotBeNull();
    }

    public void Test_Procedure_CannotRun_NoDomain()
    {
        var descriptor = this.engine.Describe(typeof(NoDomainQuery));
        var args = new ProcArgs(typeof(object), typeof(object));

        Should.Throw<ProcExecutionException>(() => this.engine.Proc(descriptor, args));
    }

    public void Test_Procedure_CanDescribe()
    {
        var descriptor = this.engine.Describe(typeof(BatchedCommand));
        var args = new ProcArgs(typeof(int), typeof(object));

        this.engine.Proc(descriptor, args).ShouldNotBeNull();
    }

    public void Test_Describe_IsCached()
    {
        this.engine.Describe(typeof(BatchedCommand)).ShouldBeSameAs(new ProcEngine(null).Describe(typeof(BatchedCommand)));
    }

    public void Test_Page_ResultVariance()
    {
        var descriptor = this.engine.Describe(typeof(BatchedCommand));
        var args1 = new ProcArgs(typeof(object), typeof(int));
        var args2 = new ProcArgs(typeof(object), typeof(string));

        this.engine.Proc(descriptor, args1).ShouldNotBeNull();
        this.engine.Proc(descriptor, args2).ShouldNotBeNull();
    }
}
