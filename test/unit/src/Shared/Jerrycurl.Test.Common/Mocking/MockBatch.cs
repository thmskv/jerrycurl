using System.Data;
using Jerrycurl.Cqs.Sessions;

namespace Jerrycurl.Test
{
    public class MockBatch : IBatch
    {
        private readonly string commandText;

        public object Source => null;

        public MockBatch(string commandText)
        {
            this.commandText = commandText;
        }

        public void Build(IDbCommand adoCommand)
        {
            adoCommand.CommandText = this.commandText;
        }
    }
}
