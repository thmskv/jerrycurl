using Jerrycurl.Mvc.Test.Project.Accessors;
using Shouldly;
using System;

namespace Jerrycurl.Mvc.Test
{
    public class LocatorTests
    {
        private readonly ProcLocator locator = new ProcLocator();
        private readonly Type accessorType = typeof(LocatorAccessor);

        public void Test_FindPage_CaseInsensitive()
        {
            var query = this.FindPage("locatorquery2");

            query.PageType.ShouldBe(typeof(Project.Queries.Locator.LocatorQuery2_cssql));
        }

        public void Test_FindPage_FromAccessor()
        {
            var query = this.FindPage("LocatorQuery2");
            var command = this.FindPage("LocatorCommand1");

            query.PageType.ShouldBe(typeof(Project.Queries.Locator.LocatorQuery2_cssql));
            command.PageType.ShouldBe(typeof(Project.Commands.Locator.LocatorCommand1_cssql));
        }

        public void Test_FindPage_FromPage()
        {
            var query = this.FindPage("LocatorQuery4", typeof(Project.Queries.Locator.LocatorQuery2_cssql));
            var command = this.FindPage("LocatorCommand3", typeof(Project.Commands.Locator.LocatorCommand1_cssql));

            query.PageType.ShouldBe(typeof(Project.Queries.LocatorQuery4_cssql));
            command.PageType.ShouldBe(typeof(Project.Commands.LocatorCommand3_cssql));
        }

        public void Test_FindPage_InSharedRoot()
        {
            var query = this.FindPage("LocatorQuery4");
            var command = this.FindPage("LocatorCommand3");

            query.PageType.ShouldBe(typeof(Project.Queries.LocatorQuery4_cssql));
            command.PageType.ShouldBe(typeof(Project.Commands.LocatorCommand3_cssql));
        }

        public void Test_FindPage_InSubFolder()
        {
            var query = this.FindPage("SubFolder1/SubFolder2/LocatorQuery1");

            query.PageType.ShouldBe(typeof(Project.Queries.Locator.SubFolder1.SubFolder2.LocatorQuery1_cssql));
        }

        public void Test_FindPage_InSharedFolder()
        {
            var query = this.FindPage("LocatorQuery3");
            var command = this.FindPage("LocatorCommand2");

            query.PageType.ShouldBe(typeof(Project.Queries.Shared.LocatorQuery3_cssql));
            command.PageType.ShouldBe(typeof(Project.Commands.Shared.LocatorCommand2_cssql));
        }

        public void Test_FindPage_NotExists()
        {
            Should.Throw<PageNotFoundException>(() => this.FindPage("LocatorQueryX", this.accessorType));
        }

        public void Test_FindPage_RelativePath()
        {
            var page = this.FindPage("../Queries/Locator/SubFolder1/./SubFolder2/../../LocatorQuery2");

            page.ShouldNotBeNull();
            page.PageType.ShouldBe(typeof(Project.Queries.Locator.LocatorQuery2_cssql));
        }

        public void Test_FindPage_AbsolutePath()
        {
            var page = this.FindPage("/Jerrycurl/Mvc/Test/Project/Queries/Locator/LocatorQuery2.cssql");

            page.ShouldNotBeNull();
            page.PageType.ShouldBe(typeof(Project.Queries.Locator.LocatorQuery2_cssql));
        }
        public void Test_FindPage_DomainPath()
        {
            var page = this.FindPage("~/Queries/Locator/LocatorQuery2.cssql");

            page.ShouldNotBeNull();
            page.PageType.ShouldBe(typeof(Project.Queries.Locator.LocatorQuery2_cssql));
        }

        private PageDescriptor FindPage(string procName, Type originType = null) => this.locator.FindPage(procName, originType ?? this.accessorType);
    }
}
