using System;
using System.Reflection;

namespace Jerrycurl.Relations.Metadata
{
    public interface IRelationContract
    {
        Type ItemType { get; }
        string ItemName { get; }
        MethodInfo WriteIndex { get; }
        MethodInfo ReadIndex { get; }
    }
}
