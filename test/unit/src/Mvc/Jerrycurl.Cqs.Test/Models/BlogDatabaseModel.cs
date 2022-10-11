using System.Collections.Generic;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Cqs.Test.Models
{
    public class BlogDatabaseModel
    {
        public List<BlogView> Blogs { get; set; }
        public List<BlogPost> Posts { get; set; }
        public List<BlogCategory> Categories { get; set; }

        public class BlogView : Blog
        {
            public BlogAuthor Author { get; set; }
        }
    }
}
