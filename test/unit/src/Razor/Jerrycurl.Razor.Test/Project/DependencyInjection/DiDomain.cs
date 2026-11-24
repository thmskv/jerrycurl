using Jerrycurl.Razor.Test.Project.DependencyInjection.Services;

namespace Jerrycurl.Razor.Test.Project.DependencyInjection;

public class DiDomain : IDomain
{
    private readonly MyService service;

    public DiDomain(MyService service)
    {
        this.service = service;
    }

    public void Configure(DomainOptions options)
    {
        options.UseSqlite(this.service.ConnectionString);
    }
}
