using Jerrycurl.Relations;

namespace Jerrycurl.Razor.Projections;

public interface IProjectionData
{
    public IField Source { get; }
    public IField Input { get; }
    public IField Output { get; }
}
