namespace Jerrycurl.Razor;

public interface IProcExecutionStack
{
    public IPageExecutionContext Current { get; }

    public void Push(IPageExecutionContext context);
    public IPageExecutionContext Pop();
}
