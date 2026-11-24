using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Cqs.Metadata;

public interface IBindingMetadataBuilder : IMetadataBuilder<IBindingMetadata>
{
    public ConventionList<IBindingConvention> Conventions { get; }
    public IBindingConvention DefaultConvention { get; set; }
}
