using Jerrycurl.Cqs.Filters;
using System;
using System.Transactions;
using IsolationLevel = System.Data.IsolationLevel;

namespace Jerrycurl.Mvc;

public static class SqlOptionsExtensions
{
    public static SqlOptions UseFilters(this SqlOptions options, params IFilter[] filters)
    {
        foreach (var filter in filters)
            options.Filters.Add(filter);

        return options;
    }
    public static SqlOptions UseTransaction(this SqlOptions options) => options.UseFilters(new TransactionFilter());
    public static SqlOptions UseTransaction(this SqlOptions options, IsolationLevel isolationLevel) => options.UseFilters(new TransactionFilter(isolationLevel));

    public static SqlOptions UseTransactionScope(this SqlOptions options) => options.UseFilters(new TransactionScopeFilter());
    public static SqlOptions UseTransactionScope(this SqlOptions options, TransactionScopeOption scopeOption) => options.UseFilters(new TransactionScopeFilter(scopeOption));
    public static SqlOptions UseTransactionScope(this SqlOptions options, Func<TransactionScope> scopeFactory) => options.UseFilters(new TransactionScopeFilter(scopeFactory));
}
