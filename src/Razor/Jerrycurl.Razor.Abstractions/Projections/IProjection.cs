using System;
using System.Collections.Generic;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Razor.Metadata;

namespace Jerrycurl.Razor.Projections;

/// <summary>
/// Represents an immutable projection buffer comprised of the concatenation of a collection of attributes.
/// </summary>
public interface IProjection : ISqlWritable
{
    public ProjectionIdentity Identity { get; }
    public IEnumerable<IProjectionAttribute> Header { get; }
    public IProjectionMetadata Metadata { get; }
    public IProjectionData Data { get; }
    public IProcContext Context { get; }
    public IProjectionOptions Options { get; }

    public IProjection Append(IEnumerable<IParameter> parameters);
    public IProjection Append(IEnumerable<IUpdateBinding> bindings);
    public IProjection Append(string text);
    public IProjection Append(params IParameter[] parameter);
    public IProjection Append(params IUpdateBinding[] bindings);

    public IProjection Map(Func<IProjectionAttribute, IProjectionAttribute> mapperFunc);

    public IProjection With(IProjectionMetadata metadata = null,
                     IProjectionData data = null,
                     IEnumerable<IProjectionAttribute> header = null,
                     IProjectionOptions options = null);
}
