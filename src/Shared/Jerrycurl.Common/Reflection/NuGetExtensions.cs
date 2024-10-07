using System.Linq;
using System.Reflection;

namespace Jerrycurl.Reflection;

internal static class NuGetExtensions
{
    public static NuGetVersion GetNuGetPackageVersion(this Assembly assembly)
    {
        string infoVersion = GetNuGetPackageVersionFromMetadata(assembly) ?? assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(infoVersion))
            return null;

        int plusIndex = infoVersion.IndexOf('+');
        int dashIndex = infoVersion.IndexOf('-');

        return new NuGetVersion()
        {
            Version = infoVersion.Replace("+", ".g"),
            IsPrerelease = (dashIndex > -1),
            CommitHash = plusIndex > -1 ? infoVersion.Substring(plusIndex + 1) : null,
            PublicVersion = plusIndex > -1 ? infoVersion.Remove(plusIndex) : infoVersion,
        };
    }

    private static string GetNuGetPackageVersionFromMetadata(Assembly assembly)
    {
        var attribute = assembly?.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(m => m.Key == "NuGetPackageVersion");

        return attribute?.Value;
    }
}
