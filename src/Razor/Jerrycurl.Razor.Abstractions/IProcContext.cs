namespace Jerrycurl.Razor;

public interface IProcContext
{
    public IDomainOptions Domain { get; }
    public IProcRenderer Renderer { get; }
    public IProcLookup Lookup { get; }
    public IPageExecutionContext Execution { get; }
}
