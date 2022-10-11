using System.Data;

namespace Jerrycurl.Cqs.Queries.Internal.Compilation
{
    internal delegate TItem EnumerateFactory<TItem>(IDataReader dataReader);
}
