using System;
using System.Collections.Generic;

namespace Jerrycurl.Relations.Metadata
{
    public interface ISchemaStore
    {
        DotNotation Notation { get; }
        ISchema GetSchema(Type modelType);
        IEnumerable<IMetadataBuilder> Builders { get; }
    }
}
