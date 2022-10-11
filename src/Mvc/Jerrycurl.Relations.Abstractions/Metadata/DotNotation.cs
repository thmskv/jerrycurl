using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Jerrycurl.Relations.Metadata
{
    public class DotNotation
    {
        public virtual IEqualityComparer<string> Comparer { get; }
        public virtual char IndexBefore { get; } = '[';
        public virtual char IndexAfter { get; } = ']';
        public virtual char Dot { get; } = '.';

        public DotNotation()
            : this(StringComparer.OrdinalIgnoreCase)
        {

        }

        public DotNotation(IEqualityComparer<string> comparer)
        {
            this.Comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        public string Combine(params string[] parts)
            => string.Join(this.Dot.ToString(), parts.Where(p => p.Length > 0));

        public string Combine(string part1, string part2)
        {
            if (part1 == null || part2 == null)
                return null;
            else if (part1 == "")
                return part2;
            else if (part2 == "")
                return part1;

            return $"{part1}{this.Dot}{part2}";
        }

        public string Model() => "";
        public string Index(string name, int index) => $"{name}{this.IndexBefore}{index}{this.IndexAfter}";
        public bool Equals(string name1, string name2) => this.Comparer.Equals(name1, name2);

        public string Lambda(LambdaExpression expression)
        {
            Stack<string> stack = new Stack<string>();
            Expression current = expression?.Body;

            while (current != null)
            {
                switch (current.NodeType)
                {
                    case ExpressionType.MemberAccess when current is MemberExpression memberExpression:
                        stack.Push(memberExpression.Member.Name);
                        current = memberExpression.Expression;
                        break;
                    case ExpressionType.Parameter:
                        return this.Combine(stack.ToArray());
                    default:
                        current = null;
                        break;
                }
            }

            return null;
        }

        public string Path(string from, string to)
        {
            if (from == null || to == null)
                return null;
            else if (this.Comparer.Equals(from, to))
                return "";
            else if (this.Comparer.Equals(from, this.Model()))
                return to;
            else if (to.Length < from.Length + 2 || !this.Comparer.Equals(from, to.Substring(0, from.Length)) || to[from.Length] != this.Dot)
                return null;

            return to.Remove(0, from.Length + 1);
        }

        public string Parent(string name)
        {
            if (this.Comparer.Equals(name, this.Model()) || name == null)
                return null;

            string[] parts = name.Split(new[] { this.Dot });

            return this.Combine(parts.Take(parts.Length - 1).ToArray());
        }

        public string Member(string name)
        {
            string[] parts = name?.Split(new[] { this.Dot });

            return parts?.Last();
        }

        public int Depth(string name)
        {
            if (name == null)
                return -1;
            else if (this.Comparer.Equals(name, this.Model()))
                return 0;

            string[] parts = name.Split(new[] { this.Dot });

            return parts.Length;
        }
    }
}
