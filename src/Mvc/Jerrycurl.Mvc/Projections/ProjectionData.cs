using System;
using System.Linq;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations;

namespace Jerrycurl.Mvc.Projections
{
    public class ProjectionData : IProjectionData
    {
        public IField Source { get; }
        public IField Input { get; }
        public IField Output { get; }

        public ProjectionData(IField value)
        {
            this.Source = this.Input = this.Output = value ?? throw new ArgumentNullException(nameof(value));
        }

        public ProjectionData(IField source, IField input, IField output)
        {
            this.Source = source ?? throw new ArgumentNullException(nameof(source));
            this.Input = input ?? throw new ArgumentNullException(nameof(input));
            this.Output = output ?? throw new ArgumentNullException(nameof(output));
        }

        internal static IProjectionData Resolve(IProjectionData data, IProjectionMetadata metadata)
        {
            if (data == null)
                return null;
            else if (data.Source.Metadata == metadata.Relation)
                return data;
            else
                return ProjectionReader.Lookup(data.Source, new[] { metadata }).FirstOrDefault();
        }

        internal static IProjectionData Resolve(ProjectionIdentity identity)
        {
            if (identity.Source != null)
                return new ProjectionData(identity.Source);

            return null;
        }
    }
}