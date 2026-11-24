namespace Jerrycurl.Razor;

internal class ProcContext : IProcContext
{
    public IDomainOptions Domain { get; }
    public IProcRenderer Renderer { get; }
    public IProcLookup Lookup { get; }
    public IPageExecutionContext Execution => this.Stack.Current;
    public ProcExecutionStack Stack { get; }

    public ProcContext(PageDescriptor descriptor, IDomainOptions domain)
    {
        this.Domain = domain;
        this.Renderer = new ProcRenderer(this);
        this.Lookup = new ProcLookup();
        this.Stack = new ProcExecutionStack();
    }
}
