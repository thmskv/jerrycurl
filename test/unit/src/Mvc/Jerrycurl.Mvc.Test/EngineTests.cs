using Jerrycurl.Mvc.Test.Project.Accessors;
using Jerrycurl.Mvc.Test.Project2.NoDomain;
using Shouldly;

namespace Jerrycurl.Mvc.Test
{
    public class EngineTests
    {
        private readonly ProcLocator locator = new ProcLocator();
        private readonly ProcEngine engine = new ProcEngine(null);

        public void Test_Page_CanLookup_NoDomain()
        {
            var descriptor = this.locator.FindPage("NoDomainQuery", typeof(NoDomainAccessor));
            var args = new ProcArgs(typeof(object), typeof(object));

            descriptor.ShouldNotBeNull();
            descriptor.DomainType.ShouldBeNull();

            var factory = Should.NotThrow(() => this.engine.Page(descriptor.PageType));

            factory.ShouldNotBeNull();
        }

        public void Test_Procedure_CannotLookup_NoDomain()
        {
            var descriptor = this.locator.FindPage("NoDomainQuery", typeof(NoDomainAccessor));
            var args = new ProcArgs(typeof(object), typeof(object));

            descriptor.ShouldNotBeNull();
            descriptor.DomainType.ShouldBeNull();

            Should.Throw<ProcExecutionException>(() => this.engine.Proc(descriptor, args));
        }

        public void Test_Procedure_CanLookup()
        {
            var descriptor = this.FindPage("LocatorQuery2");
            var args = new ProcArgs(typeof(int), typeof(object));
            var factory = this.engine.Proc(descriptor, args);

            factory.ShouldNotBeNull();
        }

        public void Test_Page_ResultVariance()
        {
            var descriptor = this.FindPage("LocatorQuery2");
            var args1 = new ProcArgs(typeof(object), typeof(int));
            var args2 = new ProcArgs(typeof(object), typeof(string));

            this.engine.Proc(descriptor, args1).ShouldNotBeNull();
            this.engine.Proc(descriptor, args2).ShouldNotBeNull();
        }

        private PageDescriptor FindPage(string procName) => this.locator.FindPage(procName, typeof(LocatorAccessor));
    }
}
