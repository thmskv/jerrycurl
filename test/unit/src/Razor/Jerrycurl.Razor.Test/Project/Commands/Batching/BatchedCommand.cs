using Jerrycurl.Razor.Projections;

namespace Jerrycurl.Razor.Test.Project.Commands.Batching;

public class BatchedCommand : ProcPage<object, object>
{
    public BatchedCommand(IProjection model, IProjection result)
        : base(model, result)
    {

    }

    public override void Render()
    {
        for (int i = 0; i < 20; i++)
        {
            this.WriteLiteral($"BATCH {i}\r\n");
            this.Write($"Param {i}");

            this.Context.Execution.Buffer.Mark();
        }
    }
}
