namespace Jerrycurl.Razor;

public interface IProcResult
{
    public ISqlBuffer Buffer { get; }
    public IDomainOptions Domain { get; }
    public ISqlPage Page { get; }
}
