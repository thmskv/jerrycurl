namespace Jerrycurl.Razor;

public class PageExecutionContext : IPageExecutionContext
{
    public PageDescriptor Page { get; set; }
    public BodyFactory Body { get; set; }
    public ISqlBuffer Buffer { get; set; }
}
