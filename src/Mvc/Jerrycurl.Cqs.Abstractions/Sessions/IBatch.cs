using System.Data;

namespace Jerrycurl.Cqs.Sessions;

public interface IBatch
{
    public void Build(IDbCommand adoCommand);
}
