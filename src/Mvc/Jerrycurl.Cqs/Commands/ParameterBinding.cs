using System;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Relations;

namespace Jerrycurl.Cqs.Commands
{
    public class ParameterBinding : IUpdateBinding
    {
        public string ParameterName { get; }
        public IField Target { get; }

        public ParameterBinding(IField target, string parameterName)
        {
            this.Target = target ?? throw new ArgumentNullException(nameof(target));
            this.ParameterName = parameterName ?? throw new ArgumentNullException(nameof(parameterName));
        }

        public ParameterBinding(IParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            this.ParameterName = parameter.Name;
            this.Target = parameter.Source;
        }

        public override string ToString() => $"ParameterBinding: {this.ParameterName} -> {this.Target.Identity}";
    }
}
