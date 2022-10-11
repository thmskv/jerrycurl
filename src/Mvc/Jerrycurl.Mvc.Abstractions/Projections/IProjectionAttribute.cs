using System.Collections.Generic;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Sessions;
using Jerrycurl.Mvc.Metadata;

namespace Jerrycurl.Mvc.Projections
{
    public interface IProjectionAttribute : ISqlWritable
    {
        ProjectionIdentity Identity { get; }
        IProjectionMetadata Metadata { get; }
        IProjectionData Data { get; }
        IProcContext Context { get; }
        ISqlContent Content { get; }

        IProjectionAttribute Append(IEnumerable<IParameter> parameters);
        IProjectionAttribute Append(IEnumerable<IUpdateBinding> bindings);
        IProjectionAttribute Append(string text);
        IProjectionAttribute Append(params IParameter[] parameter);
        IProjectionAttribute Append(params IUpdateBinding[] bindings);

        IProjectionAttribute With(IProjectionMetadata metadata = null,
                                  IProjectionData data = null,
                                  ISqlContent content = null);
    }
}
