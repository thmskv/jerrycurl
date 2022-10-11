using System.Collections.Generic;

namespace Jerrycurl.Relations.Test.Models
{
    public class RecursiveModel
    {
        public string Name { get; set; }
        public List<RecursiveModel> Subs { get; set; }
    }
}
