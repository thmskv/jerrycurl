using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Relations.Test.Metadata
{
    public class RecursiveMetadataBuilder : IMetadataBuilder<CustomMetadata>
    {
        public CustomMetadata GetMetadata(IMetadataBuilderContext context)
        {
            if (context.Relation.Parent != null)
                return context.Relation.Parent.Identity.Schema.Lookup<CustomMetadata>();

            return null;
        }

        public void Initialize(IMetadataBuilderContext context)
        {

        }
    }
}
