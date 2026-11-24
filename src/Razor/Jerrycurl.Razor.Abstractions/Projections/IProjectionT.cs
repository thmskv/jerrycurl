using System;
using System.Collections.Generic;
using Jerrycurl.Razor.Metadata;

namespace Jerrycurl.Razor.Projections;

public interface IProjection<out TModel> : IProjection
{
    public new IProjection<TModel> Map(Func<IProjectionAttribute, IProjectionAttribute> m);
    public new IProjection<TModel> With(IProjectionMetadata metadata = null,
                                 IProjectionData data = null,
                                 IEnumerable<IProjectionAttribute> header = null,
                                 IProjectionOptions options = null);
}