using System.Collections.Generic;
using Jerrycurl.Relations;

namespace Jerrycurl.Cqs.Test.Models.Custom
{
    public class ListTypeModel
    {
        public IList<int> IList { get; set; }
        public List<int> List { get; set; }
        public IReadOnlyList<int> IReadOnlyList { get; set; }
        public IEnumerable<int> IEnumerable { get; set; }
        public One<int> One { get; set; }
        public IReadOnlyCollection<int> IReadOnlyCollection { get; set; }
    }
}
