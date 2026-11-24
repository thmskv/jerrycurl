namespace Jerrycurl.Cqs.Metadata;

public interface IBindingConvention
{
    public int Priority { get; }

    public IBindingParameterContract GetParameterContract(IBindingMetadata metadata);
    public IBindingCompositionContract GetCompositionContract(IBindingMetadata metadata);
    public IBindingValueContract GetValueContract(IBindingMetadata metadata);
    public IBindingHelperContract GetHelperContract(IBindingMetadata metadata);
}
