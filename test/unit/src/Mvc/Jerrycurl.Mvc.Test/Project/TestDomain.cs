namespace Jerrycurl.Mvc.Test.Project
{
    public class TestDomain : IDomain
    {
        public void Configure(DomainOptions options)
        {
            options.UseSqlite("DATA SOURCE=testmvc.db");
        }
    }
}
