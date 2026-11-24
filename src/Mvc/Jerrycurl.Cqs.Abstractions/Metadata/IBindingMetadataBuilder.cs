using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata;

public interface IBindingMetadataBuilder : IMetadataBuilder<IBindingMetadata>
{
    ConventionList<IBindingConvention> Conventions { get; }
    IBindingConvention DefaultConvention { get; set; }
}
