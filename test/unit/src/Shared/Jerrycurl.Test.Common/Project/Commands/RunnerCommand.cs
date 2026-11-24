using Jerrycurl.Razor;
using Jerrycurl.Razor.Projections;
using Jerrycurl.Test.Project.Models;

namespace Jerrycurl.Test.Project.Commands;

/// <summary>
/// Renders any <see cref="IRunnable"/> as a command. Hand-written equivalent of a generated <c>@command</c> page.
/// </summary>
public class RunnerCommand : ProcPage<IRunnable, object>
{
    public RunnerCommand(IProjection model, IProjection result)
        : base(model, result)
    {

    }

    public override void Render() => this.Model.Execute(this);

    public static void Execute(IRunnable model) => ExecuteCore(typeof(RunnerCommand), model);
}
