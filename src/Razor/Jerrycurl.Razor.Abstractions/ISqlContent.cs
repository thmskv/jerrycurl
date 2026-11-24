using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Sessions;
using System.Collections.Generic;

namespace Jerrycurl.Razor;

public interface ISqlContent : ISqlWritable
{
    public IEnumerable<IUpdateBinding> Bindings { get; }
    public IEnumerable<IParameter> Parameters { get; }
    public string Text { get; }

    public ISqlContent Append(IEnumerable<IParameter> parameters);
    public ISqlContent Append(IEnumerable<IUpdateBinding> bindings);
    public ISqlContent Append(params IParameter[] parameter);
    public ISqlContent Append(params IUpdateBinding[] bindings);
    public ISqlContent Append(string text);
}
