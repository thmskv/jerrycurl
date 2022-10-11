using Jerrycurl.Relations.Metadata;
using Jerrycurl.Cqs.Metadata;

namespace Jerrycurl.Mvc.Metadata
{
    public static class MetadataExtensions
    {
        public static bool HasFlag(this IProjectionMetadata metadata, TableMetadataFlags flag)
        {
            if (metadata.Table != null && metadata.Table.HasFlag(flag))
                return true;
            else if (metadata.Column != null && metadata.Column.HasFlag(flag))
                return true;

            return false;
        }
        public static bool HasAnyFlag(this IProjectionMetadata metadata, TableMetadataFlags flag)
        {
            if (metadata.Table != null && metadata.Table.HasAnyFlag(flag))
                return true;
            else if (metadata.Column != null && metadata.Column.HasAnyFlag(flag))
                return true;

            return false;
        }

        public static bool HasFlag(this IProjectionMetadata metadata, RelationMetadataFlags flag) => (metadata.Relation != null && metadata.Relation.HasFlag(flag));
        public static bool HasAnyFlag(this IProjectionMetadata metadata, RelationMetadataFlags flag) => (metadata.Relation != null && metadata.Relation.HasAnyFlag(flag));

        public static bool HasFlag(this IProjectionMetadata metadata, ReferenceMetadataFlags flag) => (metadata.Relation != null && metadata.Reference.HasFlag(flag));
        public static bool HasAnyFlag(this IProjectionMetadata metadata, ReferenceMetadataFlags flag) => (metadata.Relation != null && metadata.Reference.HasAnyFlag(flag));

        public static bool HasFlag(this IProjectionMetadata metadata, ProjectionMetadataFlags flag) => (metadata.Flags & flag) == flag;
        public static bool HasAnyFlag(this IProjectionMetadata metadata, ProjectionMetadataFlags flag) => (metadata.Flags & flag) != ProjectionMetadataFlags.None;
    }
}
