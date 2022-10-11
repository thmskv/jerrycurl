using System;
using Jerrycurl.Relations;

namespace Jerrycurl.Cqs.Commands
{
    public class CascadeBinding : IUpdateBinding
    {
        public IField Source { get; }
        public IField Target { get; }

        public CascadeBinding(IField target, IField source)
        {
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public override string ToString() => $"CascadeBinding: {this.Source.Identity} -> {this.Target.Identity}";
    }
}
