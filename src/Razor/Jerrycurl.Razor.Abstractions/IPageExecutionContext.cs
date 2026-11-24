namespace Jerrycurl.Razor;

public interface IPageExecutionContext
{
    public PageDescriptor Page { get; }
    public ISqlBuffer Buffer { get; }
    public BodyFactory Body { get; }
}
