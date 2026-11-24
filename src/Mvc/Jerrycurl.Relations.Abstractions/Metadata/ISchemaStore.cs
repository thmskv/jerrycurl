using System;
using System.Collections.Generic;

namespace Jerrycurl.Relations.Metadata;

public interface ISchemaStore
{
    public DotNotation Notation { get; }
    public ISchema GetSchema(Type modelType);
    public IEnumerable<IMetadataBuilder> Builders { get; }
}
