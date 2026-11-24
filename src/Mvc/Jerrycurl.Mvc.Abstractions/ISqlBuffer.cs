using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Sessions;
using System.Collections.Generic;

namespace Jerrycurl.Mvc;

public interface ISqlBuffer
{
    public void Append(IEnumerable<IParameter> parameters);
    public void Append(IEnumerable<IUpdateBinding> bindings);
    public void Append(string text);
    public void Append(ISqlContent content);

    public void Push(int batchIndex);
    public void Pop();
    public void Mark();

    public IEnumerable<ISqlContent> Read(ISqlOptions options);
    public ISqlContent ReadToEnd();
}
