namespace Jerrycurl.Mvc.Test.Project.DependencyInjection.Services
{
    public class MyService
    {
        public string SomeValue { get; set; } = "SOMEVALUE";
        public string ConnectionString { get; } = "DATA SOURCE=testmvc.db";
    }
}
