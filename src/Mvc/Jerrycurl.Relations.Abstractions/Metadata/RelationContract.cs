using System;
using System.Reflection;

namespace Jerrycurl.Relations.Metadata
{
    public class RelationContract : IRelationContract
    {
        public Type ItemType { get; set; }
        public string ItemName { get; set; } = "Item";
        public MethodInfo WriteIndex { get; set; }
        public MethodInfo ReadIndex { get; set; }
    }
}
