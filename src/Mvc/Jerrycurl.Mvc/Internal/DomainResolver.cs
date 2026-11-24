using System;
using System.Linq;
using Jerrycurl.CodeAnalysis;

namespace Jerrycurl.Mvc.Internal;

internal static class DomainResolver
{
    /// <summary>
    /// Finds the single <see cref="IDomain"/> type in the namespace of <paramref name="originType"/> or the closest parent namespace.
    /// </summary>
    public static Type GetDomainType(Type originType)
    {
        if (IsDomainType(originType))
            return originType;

        Type[] allTypes = [.. originType.Assembly.DefinedTypes.Select(ti => ti.AsType())];

        foreach (Namespace ns in Namespace.FromType(originType).Traverse())
        {
            Type[] domainTypes = [.. allTypes.Where(t => t.Namespace == ns.Definition && IsDomainType(t))];

            if (domainTypes.Length == 1)
                return domainTypes[0];
            else if (domainTypes.Length > 1)
                throw new InvalidOperationException($"Multiple domains found in namespace '{ns}'.");
        }

        return null;
    }

    private static bool IsDomainType(Type type) => typeof(IDomain).IsAssignableFrom(type);
}
