using Jerrycurl.Mvc.Projections;
using System;

namespace Jerrycurl.Mvc;

internal class ProcRenderer : IProcRenderer
{
    public ProcContext Context { get; }

    public ProcRenderer(ProcContext context)
    {
        this.Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public ISqlContent Body()
    {
        if (this.Context.Stack.IsEmpty)
            throw new ProcExecutionException("Execution stack is empty. Please only call Body() on template pages.");

        this.Context.Execution.Body?.Invoke();

        return SqlContent.Empty;
    }

    public ISqlContent Partial(Type pageType, IProjection model, IProjection result)
    {
        ArgumentNullException.ThrowIfNull(pageType);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(result);

        PageDescriptor descriptor = this.Context.Domain.Engine.Describe(pageType);
        PageFactory factory = this.Context.Domain.Engine.Page(descriptor.PageType);

        this.Context.Stack.Push(new PageExecutionContext() { Page = descriptor });

        factory(model, result);

        this.Context.Stack.Pop();

        return SqlContent.Empty;
    }
}
