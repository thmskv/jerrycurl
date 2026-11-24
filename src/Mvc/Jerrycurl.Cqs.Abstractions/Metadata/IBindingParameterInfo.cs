using System.Data;
using Jerrycurl.Relations;

namespace Jerrycurl.Cqs.Metadata;

public interface IBindingParameterInfo
{
    public IBindingMetadata Metadata { get; }
    public IDbDataParameter Parameter { get; }
    public IField Field { get; }
}
