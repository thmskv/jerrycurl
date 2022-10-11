using System.Data;

namespace Jerrycurl.Cqs.Commands.Internal.Compilation
{
    internal delegate void BufferWriter(IDataReader dataReader, FieldBuffer[] buffers);
}
