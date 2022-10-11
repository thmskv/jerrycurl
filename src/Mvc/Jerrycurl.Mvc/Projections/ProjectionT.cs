using System;
using System.Collections.Generic;
using Jerrycurl.Mvc.Metadata;

namespace Jerrycurl.Mvc.Projections
{
    public class Projection<TModel> : Projection, IProjection<TModel>
    {
        public Projection(IProjection projection)
            : base(projection)
        {

        }

        public Projection(ProjectionIdentity identity, IProcContext context, IProjectionMetadata metadata)
            : base(identity, context, metadata)
        {

        }

        public new IProjection<TModel> Map(Func<IProjectionAttribute, IProjectionAttribute> m)
            => new Projection<TModel>(base.Map(m));

        public new IProjection<TModel> With(IProjectionMetadata metadata = null,
                                            IProjectionData data = null,
                                            IEnumerable<IProjectionAttribute> header = null,
                                            IProjectionOptions options = null)
            => new Projection<TModel>(base.With(metadata, data, header, options));
    }
}
