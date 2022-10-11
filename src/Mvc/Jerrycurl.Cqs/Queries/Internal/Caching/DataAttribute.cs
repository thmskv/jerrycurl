using System;
using System.Diagnostics;

namespace Jerrycurl.Cqs.Queries.Internal.Caching
{
    [DebuggerDisplay("{GetType().Name,nq}: {Name}")]
    internal class DataAttribute
    {
        public string Name { get; }

        public DataAttribute(string name)
        {
            this.Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}
