using System.Collections.Generic;
using System.Text;

namespace Jerrycurl.CodeAnalysis.Razor.Generation;

/// <summary>
/// Splits a C# parameter-like list (e.g. <c>@input Dictionary&lt;int, string&gt; Map, int Id = 5</c>) on top-level commas,
/// ignoring commas nested in <c>()</c>, <c>[]</c>, <c>{}</c>, string/char literals, and <c>&lt;&gt;</c> in the declaration part.
/// </summary>
internal static class ArgumentSplitter
{
    public static IList<string> SplitArguments(string text)
    {
        List<string> parts = [];

        if (string.IsNullOrWhiteSpace(text))
            return parts;

        StringBuilder current = new StringBuilder();
        int depth = 0;
        bool inDefault = false;
        char? quote = null;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            current.Append(c);

            if (quote != null)
            {
                if (c == '\\' && i + 1 < text.Length)
                    current.Append(text[++i]);
                else if (c == quote)
                    quote = null;
            }
            else if (c == '"' || c == '\'')
                quote = c;
            else if (c == '(' || c == '[' || c == '{' || (c == '<' && !inDefault))
                depth++;
            else if ((c == ')' || c == ']' || c == '}' || (c == '>' && !inDefault)) && depth > 0)
                depth--;
            else if (c == '=' && depth == 0)
                inDefault = true;
            else if (c == ',' && depth == 0)
            {
                current.Length--;
                parts.Add(current.ToString());
                current.Clear();
                inDefault = false;
            }
        }

        parts.Add(current.ToString());
        parts.RemoveAll(string.IsNullOrWhiteSpace);

        return parts;
    }

    /// <summary>
    /// Splits <c>int Id = 5</c> into <c>("int Id", "5")</c>; the default is <c>null</c> when absent.
    /// </summary>
    public static (string Declaration, string Default) SplitDefault(string argument)
    {
        int depth = 0;
        char? quote = null;

        for (int i = 0; i < argument.Length; i++)
        {
            char c = argument[i];

            if (quote != null)
            {
                if (c == '\\')
                    i++;
                else if (c == quote)
                    quote = null;
            }
            else if (c == '"' || c == '\'')
                quote = c;
            else if (c == '(' || c == '[' || c == '{' || c == '<')
                depth++;
            else if ((c == ')' || c == ']' || c == '}' || c == '>') && depth > 0)
                depth--;
            else if (c == '=' && depth == 0)
                return (argument.Substring(0, i).Trim(), argument.Substring(i + 1).Trim());
        }

        return (argument.Trim(), null);
    }
}
