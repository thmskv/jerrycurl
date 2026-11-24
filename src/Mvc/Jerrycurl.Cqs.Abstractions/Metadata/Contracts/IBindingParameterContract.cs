namespace Jerrycurl.Cqs.Metadata;

public delegate void BindingParameterWriter(IBindingParameterInfo parameterInfo);
public delegate object BindingParameterConverter(object value);

public interface IBindingParameterContract
{
    public BindingParameterWriter Write { get; }
    public BindingParameterConverter Convert { get; }
}
