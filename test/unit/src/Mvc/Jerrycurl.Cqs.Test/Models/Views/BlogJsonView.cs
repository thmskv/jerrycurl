using Jerrycurl.Cqs.Metadata.Annotations;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Cqs.Test.Models.Views
{
    internal class BlogJsonView
    {
        [Json]
        public Blog Blog { get; set; }
    }
}
