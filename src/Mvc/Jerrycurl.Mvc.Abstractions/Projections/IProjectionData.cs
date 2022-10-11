using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.Projections
{
    public interface IProjectionData
    {
        public IField Source { get; }
        public IField Input { get; }
        public IField Output { get; }
    }
}
