using Jerrycurl.CommandLine;
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
    public class NuGetLoaderContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver resolver;

        public NuGetLoaderContext(string binPath)
        {
            this.resolver = new AssemblyDependencyResolver(binPath);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = this.resolver.ResolveAssemblyToPath(assemblyName);

            if (assemblyPath != null)
                return this.LoadFromAssemblyPath(assemblyPath);

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = this.resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

            if (libraryPath != null)
                return this.LoadUnmanagedDllFromPath(libraryPath);

            return IntPtr.Zero;
        }
    }
}
