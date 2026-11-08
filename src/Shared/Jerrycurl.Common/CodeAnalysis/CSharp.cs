using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Jerrycurl.CodeAnalysis;

internal static class CSharp
{
    public static string Identifier(string s)
    {
        if (string.IsNullOrEmpty(s))
            throw new ArgumentException("Identifier cannot be empty.", nameof(s));

        string identifier = new string([.. s.Select(c => IsIdentifierPart(c) ? c : '_')]);

        if (!IsIdentifierStart(s[0]))
            identifier = $"_{identifier}";

        return identifier;
    }

    private static readonly HashSet<string> keywords = new HashSet<string>(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
        "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
        "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
    };

    /// <summary>
    /// Prefixes reserved C# keywords with '@' so they can be emitted as identifiers in generated code.
    /// </summary>
    public static string EscapeKeyword(string identifier)
        => identifier != null && keywords.Contains(identifier) ? "@" + identifier : identifier;

    /// <summary>
    /// Escapes each part of a dot-separated name (e.g. a namespace) for use in generated code.
    /// </summary>
    public static string QualifiedName(string name)
        => name == null ? null : string.Join(".", name.Split('.').Select(EscapeKeyword));

    /// <summary>
    /// Formats a file name for #line and #pragma checksum directives, which do not process escape sequences.
    /// </summary>
    public static string FileName(string s)
    {
        if (s == null)
            return "\"\"";

        StringBuilder builder = new StringBuilder(s.Length + 2);

        foreach (char c in s)
        {
            if (c != '"' && c != '\r' && c != '\n')
                builder.Append(c);
        }

        return $"\"{builder}\"";
    }

    public static string Literal(string s)
    {
        if (s == null)
            return "null";

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < s.Length; i++)
        {
            char c0 = s[i];

            switch (c0)
            {
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\0':
                    builder.Append("\\0");
                    break;
                case '\a':
                    builder.Append("\\a");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\v':
                    builder.Append("\\v");
                    break;
                case '\"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                default:
                    builder.Append(c0);
                    break;
            }
        }

        return $"\"{builder}\"";
    }


    private static bool IsIdentifierStart(char c)
    {
        return (char.IsLetter(c) || c == '_' || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.LetterNumber);
    }

    private static bool IsIdentifierPart(char c)
    {
        if (char.IsDigit(c))
            return true;
        else if (IsIdentifierStart(c))
            return true;
        else
        {
            switch (CharUnicodeInfo.GetUnicodeCategory(c))
            {
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.Format:
                    return true;
            }
        }

        return false;
    }
}
