namespace Jerrycurl.Razor;

public interface IDialect
{
    public string Identifier(string id);
    public string Parameter(string parameterName);
    public string Variable(string variableName);
    public string Literal(object value);
    public string String(string value);

    public string Qualifier { get; }

    public DialectSupport Support { get; }
}
