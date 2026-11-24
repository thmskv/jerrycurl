using System.Data;
using Jerrycurl.Relations;

namespace Jerrycurl.Cqs.Sessions;

public interface IParameter
{
    public string Name { get; }
    public IField Source { get; }

    public void Build(IDbDataParameter adoParameter);
}
