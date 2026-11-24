namespace Jerrycurl.Mvc;

public interface IProcResult
{
    public ISqlBuffer Buffer { get; }
    public IDomainOptions Domain { get; }
    public ISqlPage Page { get; }
}
