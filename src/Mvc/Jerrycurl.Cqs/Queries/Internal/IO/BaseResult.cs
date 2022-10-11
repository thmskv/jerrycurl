using System;
using System.Collections.Generic;
using System.Diagnostics;
using Jerrycurl.Cqs.Queries.Internal.IO.Writers;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Queries.Internal.IO
{
    [DebuggerDisplay("{GetType().Name,nq}: {Schema,nq}")]
    internal abstract class BaseResult
    {
        public ISchema Schema { get; }
        public List<HelperWriter> Helpers { get; set; } = new List<HelperWriter>();

        public BaseResult(ISchema schema)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }
    }
}
