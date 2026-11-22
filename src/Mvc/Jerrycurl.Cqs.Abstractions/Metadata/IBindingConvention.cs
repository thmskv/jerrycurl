namespace Jerrycurl.Cqs.Metadata;

public interface IBindingConvention
{
    int Priority { get; }

    IBindingParameterContract GetParameterContract(IBindingMetadata metadata);
    IBindingCompositionContract GetCompositionContract(IBindingMetadata metadata);
    IBindingValueContract GetValueContract(IBindingMetadata metadata);
    IBindingHelperContract GetHelperContract(IBindingMetadata metadata);
}
