using System;
using System.Threading.Tasks;

namespace Jerrycurl.Cqs.Filters;

public interface IFilterAsyncHandler : IAsyncDisposable
{
    public Task OnConnectionOpeningAsync(FilterContext context);
    public Task OnConnectionOpenedAsync(FilterContext context);
    public Task OnConnectionClosingAsync(FilterContext context);
    public Task OnConnectionClosedAsync(FilterContext context);
    public Task OnCommandCreatedAsync(FilterContext context);
    public Task OnCommandExecutedAsync(FilterContext context);
    public Task OnExceptionAsync(FilterContext context);
}
