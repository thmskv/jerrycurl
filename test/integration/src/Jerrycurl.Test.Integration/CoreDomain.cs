using System;
using Jerrycurl.Razor;

namespace Jerrycurl.Test.Integration
{
    public class CoreDomain : IDomain
    {
        public void Configure(DomainOptions options)
        {
            Settings.InitializeDomain(options);
        }
    }
}