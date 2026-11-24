using System;

namespace Jerrycurl.Cqs.Filters;

public interface IFilterHandler : IDisposable
{
    public void OnConnectionOpening(FilterContext context);
    public void OnConnectionOpened(FilterContext context);
    public void OnConnectionClosing(FilterContext context);
    public void OnConnectionClosed(FilterContext context);
    public void OnCommandCreated(FilterContext context);
    public void OnCommandExecuted(FilterContext context);
    public void OnException(FilterContext context);
}
