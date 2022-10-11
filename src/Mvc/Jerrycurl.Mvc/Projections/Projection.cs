using System;
using System.Collections.Generic;
using System.Linq;
using Jerrycurl.Collections;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Metadata;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Mvc.Metadata;
using Jerrycurl.Relations.Metadata;

namespace Jerrycurl.Mvc.Projections
{
    public class Projection : IProjection
    {
        public ProjectionIdentity Identity { get; }
        public IProjectionMetadata Metadata { get; }
        public IProjectionData Data { get; }
        public IEnumerable<IProjectionAttribute> Header { get; }
        public IProcContext Context { get; }
        public IProjectionOptions Options { get; }

        public Projection(ProjectionIdentity identity, IProcContext context, IProjectionMetadata metadata)
        {
            this.Identity = identity ?? throw ProjectionException.ArgumentNull(nameof(identity), metadata);
            this.Context = context ?? throw ProjectionException.ArgumentNull(nameof(context), metadata);
            this.Metadata = metadata;
            this.Data = ProjectionData.Resolve(identity);
            this.Options = ProjectionOptions.Default;
            this.Header = this.CreateDefaultHeader(metadata);
        }

        protected Projection(IProjection projection)
        {
            if (projection == null)
                throw ProjectionException.ArgumentNull(nameof(projection), metadata: null);

            this.Identity = projection.Identity;
            this.Context = projection.Context;
            this.Metadata = projection.Metadata;
            this.Data = projection.Data;
            this.Header = projection.Header;
            this.Options = projection.Options;
        }

        protected Projection(IProjection projection, IProjectionMetadata metadata, IProjectionData data, IEnumerable<IProjectionAttribute> header, IProjectionOptions options)
        {
            if (projection == null)
                throw ProjectionException.ArgumentNull(nameof(projection), metadata: null);

            this.Identity = projection.Identity;
            this.Context = projection.Context;
            this.Metadata = metadata ?? throw ProjectionException.ArgumentNull(nameof(metadata), metadata);
            this.Data = data;
            this.Header = header ?? throw ProjectionException.ArgumentNull(nameof(header), metadata);
            this.Options = options ?? throw ProjectionException.ArgumentNull(nameof(options), metadata);
        }

        private IEnumerable<IProjectionMetadata> SelectAttributes(IProjectionMetadata metadata)
        {
            if (metadata.HasFlag(RelationMetadataFlags.List) && metadata.Item.HasFlag(TableMetadataFlags.Column))
                return new[] { metadata.Item };
            else if (metadata.HasFlag(RelationMetadataFlags.List) && metadata.Item.HasFlag(TableMetadataFlags.Table))
                return metadata.Item.Properties.Where(a => a.HasFlag(TableMetadataFlags.Column));
            else if (metadata.HasFlag(TableMetadataFlags.Table))
                return metadata.Properties.Where(a => a.HasFlag(TableMetadataFlags.Column));

            return metadata.Properties;
        }

        private IEnumerable<IProjectionAttribute> CreateDefaultHeader(IProjectionMetadata metadata)
        {
            ProjectionIdentity identity = this.Identity;
            IProcContext context = this.Context;
            IEnumerable<IProjectionMetadata> header = this.SelectAttributes(metadata);

            if (this.Data != null)
            {
                using ProjectionReader reader = new ProjectionReader(this.Data.Source, header);

                if (reader.Read())
                {
                    foreach (var (valueMetadata, data) in header.Zip(reader.GetData()))
                        yield return new ProjectionAttribute(identity, context, valueMetadata, data);

                    yield break;
                }
            }

            foreach (IProjectionMetadata attributeMetadata in header)
                yield return new ProjectionAttribute(identity, context, attributeMetadata, data: null);
        }

        public IProjection Map(Func<IProjectionAttribute, IProjectionAttribute> mapperFunc) => this.With(header: this.Header.Select(mapperFunc));

        public IProjection Append(IEnumerable<IParameter> parameters) => this.Map(a => a.Append(parameters));
        public IProjection Append(IEnumerable<IUpdateBinding> bindings) => this.Map(a => a.Append(bindings));
        public IProjection Append(string text) => this.Map(a => a.Append(text));
        public IProjection Append(params IParameter[] parameter) => this.Map(a => a.Append(parameter));
        public IProjection Append(params IUpdateBinding[] bindings) => this.Map(a => a.Append(bindings));

        public void WriteTo(ISqlBuffer buffer)
        {
            bool wroteFirst = false;

            foreach (IProjectionAttribute attribute in this.Header)
            {
                if (!wroteFirst)
                {
                    attribute.WriteTo(buffer);
                    wroteFirst = true;
                }
                else
                {
                    buffer.Append(this.Options.Separator);
                    attribute.WriteTo(buffer);
                }
            }
        }

        public IProjection With(IProjectionMetadata metadata = null,
                                IProjectionData data = null,
                                IEnumerable<IProjectionAttribute> header = null,
                                IProjectionOptions options = null)
        {
            IProjectionMetadata newMetadata = metadata ?? this.Metadata;
            IProjectionData newData = data ?? this.Data;
            
            IEnumerable<IProjectionAttribute> newHeader = header ?? (newMetadata != this.Metadata ? this.CreateDefaultHeader(newMetadata) : this.Header);
            IProjectionOptions newOptions = options ?? this.Options;

            return new Projection(this, newMetadata, newData, newHeader, newOptions);
        }
    }
}
