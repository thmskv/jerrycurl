using Jerrycurl.Relations;
using Jerrycurl.Relations.Metadata;
using System;

namespace Jerrycurl.Mvc.Projections
{
    public class ProjectionIdentity : IEquatable<ProjectionIdentity>
    {
        public ISchema Schema { get; }
        public IField Source { get; }

        public ProjectionIdentity(ISchema schema)
        {
            this.Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        public ProjectionIdentity(IField field)
        {
            this.Source = field ?? throw new ArgumentNullException(nameof(field));
            this.Schema = field.Metadata.Identity.Schema;
        }

        public virtual bool Equals(ProjectionIdentity other) => base.Equals(other);
    }
}
