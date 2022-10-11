using System;

namespace Jerrycurl.Cqs.Queries.Internal.Parsing
{
    internal static class TypeHelper
    {
        public static Type GetKeyType(this Type type) => Nullable.GetUnderlyingType(type) ?? type;
    }
}
