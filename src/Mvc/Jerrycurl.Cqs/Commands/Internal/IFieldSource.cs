namespace Jerrycurl.Cqs.Commands.Internal;

internal interface IFieldSource
{
    public object Value { get; }
    public bool HasChanged { get; }
}
