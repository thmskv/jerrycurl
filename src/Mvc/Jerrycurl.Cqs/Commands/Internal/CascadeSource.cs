using System.Linq;

namespace Jerrycurl.Cqs.Commands.Internal
{
    internal class CascadeSource : IFieldSource
    {
        public CascadeBinding Binding { get; }
        public CommandBuffer Buffer { get; }
        public bool HasChanged => this.GetNonCascadeSource()?.HasChanged ?? false;

        public object Value
        {
            get => this.GetNonCascadeSource()?.Value;
            set { }
        }

        public CascadeSource(CascadeBinding binding, CommandBuffer buffer)
        {
            this.Binding = binding;
            this.Buffer = buffer;
        }

        private IFieldSource GetNonCascadeSource()
        {
            IFieldSource source = this.Buffer.GetSources(this.Binding.Source).FirstOrDefault();
            IFieldSource nextSource = source;

            while (nextSource is CascadeSource cascade)
            {
                nextSource = this.Buffer.GetSources(cascade.Binding.Source).FirstOrDefault();

                if (nextSource == source)
                    return null;
            }

            return nextSource;
        }
    }
}
