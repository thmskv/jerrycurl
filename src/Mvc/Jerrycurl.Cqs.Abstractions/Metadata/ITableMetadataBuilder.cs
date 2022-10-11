using Jerrycurl.Relations.Metadata;
using System.Collections.Generic;

namespace Jerrycurl.Cqs.Metadata
{
    public interface ITableMetadataBuilder : IMetadataBuilder<ITableMetadata>, ICollection<ITableContractResolver>
    {
        ITableContractResolver DefaultResolver { get; set; }
    }
}
