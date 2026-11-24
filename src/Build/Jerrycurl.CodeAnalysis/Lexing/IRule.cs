namespace Jerrycurl.CodeAnalysis.Lexing;

public interface IRule
{
    public bool Parse(Lexer lexer);
}
