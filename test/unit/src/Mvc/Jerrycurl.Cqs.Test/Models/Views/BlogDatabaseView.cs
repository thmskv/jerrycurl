using System.Collections.Generic;
using Jerrycurl.Relations;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Cqs.Test.Models.Views
{
    public class BlogDatabaseView : Blog
    {
        public BlogAuthor Author { get; set; }
        public One<BlogCategory> Category { get; set; }
        public List<BlogPost> Posts { get; set; }
    }
}
