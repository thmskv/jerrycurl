namespace Jerrycurl.CodeAnalysis.Lexing;

public interface ISymbol
{
    public bool Parse(Tokenizer tokenizer);
}