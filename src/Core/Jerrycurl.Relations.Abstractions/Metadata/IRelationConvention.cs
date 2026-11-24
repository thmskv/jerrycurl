using System;
using System.Collections.Generic;

namespace Jerrycurl.Relations.Metadata;

public interface IRelationConvention
{
    public IRelationContract GetContract(IRelationMetadata metadata);
    public IEnumerable<Attribute> GetAnnotations(IRelationMetadata metadata);
}
