using System.Collections.Generic;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Razor.Metadata;

namespace Jerrycurl.Razor.Projections;

public interface IProjectionAttribute : ISqlWritable
{
    public ProjectionIdentity Identity { get; }
    public IProjectionMetadata Metadata { get; }
    public IProjectionData Data { get; }
    public IProcContext Context { get; }
    public ISqlContent Content { get; }

    public IProjectionAttribute Append(IEnumerable<IParameter> parameters);
    public IProjectionAttribute Append(IEnumerable<IUpdateBinding> bindings);
    public IProjectionAttribute Append(string text);
    public IProjectionAttribute Append(params IParameter[] parameter);
    public IProjectionAttribute Append(params IUpdateBinding[] bindings);

    public IProjectionAttribute With(IProjectionMetadata metadata = null,
                              IProjectionData data = null,
                              ISqlContent content = null);
}
