using Jerrycurl.Cqs.Metadata.Annotations;
using Jerrycurl.Relations;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Cqs.Test.Models.Views
{
    public class BlogPostView : BlogPost
    {
        [One]
        public Blog Blog1 { get; set; }

        public One<Blog> Blog2 { get; set; }
    }
}
