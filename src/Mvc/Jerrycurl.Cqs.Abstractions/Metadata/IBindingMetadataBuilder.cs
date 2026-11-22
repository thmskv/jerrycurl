using Jerrycurl.Relations.Metadata;
using System.Collections.Generic;

namespace Jerrycurl.Cqs.Metadata;

public interface IBindingMetadataBuilder : IMetadataBuilder<IBindingMetadata>, ICollection<IBindingConvention>
{
    IBindingConvention DefaultConvention { get; set; }
}
