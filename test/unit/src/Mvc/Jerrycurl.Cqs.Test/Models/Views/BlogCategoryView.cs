using System.Collections.Generic;
using Jerrycurl.Relations;
using Jerrycurl.Test.Models.Database;

namespace Jerrycurl.Cqs.Test.Models.Views
{
    public class BlogCategoryView : BlogCategory
    {
        public One<BlogCategoryView> Parent { get; set; }
        public List<BlogCategoryView> Children { get; set; }
    }
}
