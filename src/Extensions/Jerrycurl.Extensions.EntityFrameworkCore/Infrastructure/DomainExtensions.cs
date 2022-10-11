using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Extensions.EntityFrameworkCore.Metadata;
using Jerrycurl.Relations.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Jerrycurl.Mvc
{
    public static class DomainExtensions
    {
        public static DomainOptions UseEntityFrameworkCore<TContext>(this DomainOptions options)
            where TContext : DbContext, new()
        {
            using TContext dbContext = new TContext();

            return options.UseEntityFrameworkCore(dbContext);
        }

        public static DomainOptions UseEntityFrameworkCore(this DomainOptions options, DbContext dbContext)
        {
            EntityFrameworkCoreContractResolver resolver = new EntityFrameworkCoreContractResolver(dbContext);

            options.Use((IRelationContractResolver)resolver);
            options.Use((ITableContractResolver)resolver);

            return options;

        }
    }
}