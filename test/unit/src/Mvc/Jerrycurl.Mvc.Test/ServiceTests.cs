using Jerrycurl.Mvc.Test.Project.DependencyInjection.Queries;
using Jerrycurl.Mvc.Test.Project.Queries.Misc;
using Jerrycurl.Mvc.Test.Project.DependencyInjection;
using Jerrycurl.Mvc.Test.Project.DependencyInjection.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;

namespace Jerrycurl.Mvc.Test;

public class ServiceTests
{
    private IServiceProvider GetServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddSingleton<MyService>();
        services.AddSingleton<DiDomain>();

        return services.BuildServiceProvider();
    }

    public void Test_Projection_WithoutServiceProvider()
    {
        var engine = new ProcEngine(null);
        var page = engine.Describe(typeof(ProjectedQuery));

        ISqlContent result = engine.Proc(page, new ProcArgs(typeof(object), typeof(object)))(null).Buffer.ReadToEnd();

        result.Text.ShouldBe("PROJEXISTS");
    }

    public void Test_Injection_WithoutServiceProvider()
    {
        var engine = new ProcEngine(null);
        var page = engine.Describe(typeof(InjectedQuery));

        Should.Throw<NotSupportedException>(() => engine.Proc(page, new ProcArgs(typeof(object), typeof(object)))(null));
    }

    public void Test_ProjectionAndInjection_WithServiceProvider()
    {
        var engine = new ProcEngine(this.GetServiceProvider());
        var page = engine.Describe(typeof(DiQuery));

        ISqlContent result = engine.Proc(page, new ProcArgs(typeof(object), typeof(object)))(null).Buffer.ReadToEnd();

        result.Text.ShouldBe("SOMEVALUE+PROJ");
    }
}
