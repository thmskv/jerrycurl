using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jerrycurl.Tools.Loader
{
    public class NuGetLoaderOptions
    {
        public string Package { get; set; }
        public string Version { get; set; }
        public string BuildPath { get; set; }
        public string BinPath { get; set; }
    }
}
