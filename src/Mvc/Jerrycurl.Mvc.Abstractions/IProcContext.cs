namespace Jerrycurl.Mvc;

public interface IProcContext
{
    public IProcLocator Locator { get; }
    public IDomainOptions Domain { get; }
    public IProcRenderer Renderer { get; }
    public IProcLookup Lookup { get; }
    public IPageExecutionContext Execution { get; }
}
