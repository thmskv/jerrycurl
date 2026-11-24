namespace Jerrycurl.Razor;

internal class ProcResult : IProcResult
{
    public ISqlBuffer Buffer { get; set; }
    public IDomainOptions Domain { get; set; }
    public ISqlPage Page { get; set; }
}
