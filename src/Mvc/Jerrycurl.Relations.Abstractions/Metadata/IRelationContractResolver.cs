using System;
using System.Collections.Generic;

namespace Jerrycurl.Relations.Metadata
{
    public interface IRelationContractResolver
    {
        IRelationContract GetContract(IRelationMetadata metadata);
        IEnumerable<Attribute> GetAnnotations(IRelationMetadata metadata);
    }
}
