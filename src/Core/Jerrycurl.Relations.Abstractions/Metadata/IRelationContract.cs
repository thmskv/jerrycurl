using System;
using System.Reflection;

namespace Jerrycurl.Relations.Metadata;

public interface IRelationContract
{
    public Type ItemType { get; }
    public string ItemName { get; }
    public MethodInfo WriteIndex { get; }
    public MethodInfo ReadIndex { get; }
}
