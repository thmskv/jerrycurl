﻿using System;
using System.Threading.Tasks;

namespace Jerrycurl.Cqs.Filters
{
    public interface IFilterAsyncHandler : IAsyncDisposable
    {
        Task OnConnectionOpeningAsync(FilterContext context);
        Task OnConnectionOpenedAsync(FilterContext context);
        Task OnConnectionClosingAsync(FilterContext context);
        Task OnConnectionClosedAsync(FilterContext context);
        Task OnCommandCreatedAsync(FilterContext context);
        Task OnCommandExecutedAsync(FilterContext context);
        Task OnExceptionAsync(FilterContext context);
    }
}
