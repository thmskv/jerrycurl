using Jerrycurl.CommandLine;
using Jerrycurl.Reflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Jerrycurl.Tools.Loader
{
    public class NuGetLoader
    {
        private NuGetLoaderContext context;

        public NuGetLoaderOptions Options { get; }

        public NuGetLoader(NuGetLoaderOptions options)
        {
            this.Options = options ?? throw new ArgumentNullException(nameof(options));
            this.context = new NuGetLoaderContext(options.BinPath);
        }

        public async Task ExecuteAsync<T>(Func<T, Task> asyncFunc)
        {
            await this.CreateAndBuildAsync();

            string dllPath = Path.Combine(this.Options.BinPath, $"{this.Options.Package}.dll");
            Assembly assembly = this.context.LoadFromAssemblyPath(dllPath);
            var nuget = assembly.GetNuGetPackageVersion();

            var exp = assembly.GetExportedTypes();

            var type = assembly.GetExportedTypes().FirstOrDefault(t => typeof(T).IsAssignableFrom(t));
            var data = (T)Activator.CreateInstance(type);

            await asyncFunc(data);
        }

        private async Task CreateAndBuildAsync()
        {
            XDocument xml = new XDocument(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup", this.GetProperties().Select(t => new XElement(t.Property, t.Value))),
                    new XElement("ItemGroup",
                        new XElement("PackageReference",
                            new XAttribute("Include", "Microsoft.Data.SqlClient.SNI.Runtime"),
                            new XAttribute("Version", "5.0.1")
                        ),
                        new XElement("PackageReference",
                            new XAttribute("Include", this.Options.Package),
                            new XAttribute("Version", this.Options.Version)
                        )
                    )
                )
            );

            var projPath = Path.Combine(this.Options.BuildPath, "NuGetLoader.csproj");


            xml.Save(projPath);

            await ToolRunner.RunAsync("dotnet", new[] { "build", projPath }, this.Options.BuildPath);
        }

        private IEnumerable<(string Property, string Value)> GetProperties()
        {
            yield return ("TargetFramework", "net6.0");
            yield return ("CopyLocalLockFileAssemblies", "true");
            yield return ("AppendTargetFrameworkToOutputPath", "false");
            yield return ("AppendRuntimeIdentifierToOutputPath", "false");
            yield return ("OutputPath", this.Options.BinPath);
        }
    }
}
