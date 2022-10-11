using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Sessions;
using System.Collections.Generic;

namespace Jerrycurl.Mvc
{
    public interface ISqlBuffer
    {
        void Append(IEnumerable<IParameter> parameters);
        void Append(IEnumerable<IUpdateBinding> bindings);
        void Append(string text);
        void Append(ISqlContent content);

        void Push(int batchIndex);
        void Pop();
        void Mark();

        IEnumerable<ISqlContent> Read(ISqlOptions options);
        ISqlContent ReadToEnd();
    }
}
