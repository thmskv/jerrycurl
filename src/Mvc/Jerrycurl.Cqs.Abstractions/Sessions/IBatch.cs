using System.Data;

namespace Jerrycurl.Cqs.Sessions
{
    public interface IBatch
    {
        void Build(IDbCommand adoCommand);
    }
}
