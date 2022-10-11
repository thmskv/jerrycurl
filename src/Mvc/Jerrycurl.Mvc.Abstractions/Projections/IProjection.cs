using System;
using System.Collections.Generic;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Mvc.Metadata;

namespace Jerrycurl.Mvc.Projections
{
    /// <summary>
    /// Represents an immutable projection buffer comprised of the concatenation of a collection of attributes.
    /// </summary>
    public interface IProjection : ISqlWritable
    {
        ProjectionIdentity Identity { get; }
        IEnumerable<IProjectionAttribute> Header { get; }
        IProjectionMetadata Metadata { get; }
        IProjectionData Data { get; }
        IProcContext Context { get; }
        IProjectionOptions Options { get; }

        IProjection Append(IEnumerable<IParameter> parameters);
        IProjection Append(IEnumerable<IUpdateBinding> bindings);
        IProjection Append(string text);
        IProjection Append(params IParameter[] parameter);
        IProjection Append(params IUpdateBinding[] bindings);

        IProjection Map(Func<IProjectionAttribute, IProjectionAttribute> mapperFunc);

        IProjection With(IProjectionMetadata metadata = null,
                         IProjectionData data = null,
                         IEnumerable<IProjectionAttribute> header = null,
                         IProjectionOptions options = null);
    }
}
