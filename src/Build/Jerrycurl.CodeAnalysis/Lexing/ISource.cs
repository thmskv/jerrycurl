namespace Jerrycurl.CodeAnalysis.Lexing;

public interface ISource
{
    public char? this[int position] { get; }

    public bool Eof { get; }
    public bool Bof { get; }

    public SourceSpan Read(int length);
    public void Discard(int length);

    public string GetText(SourceSpan span);
    public string GetText();
}
