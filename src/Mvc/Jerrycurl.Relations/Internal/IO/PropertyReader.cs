using Jerrycurl.Relations.Internal.Parsing;

namespace Jerrycurl.Relations.Internal.IO
{
    internal class PropertyReader : NodeReader
    {
        public PropertyReader(Node node)
            : base(node)
        {
            
        }

        public override string ToString() => this.Metadata.Identity.Name;
    }
}
