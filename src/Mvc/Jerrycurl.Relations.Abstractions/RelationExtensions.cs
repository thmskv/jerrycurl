using System.Collections.Generic;
using System.Linq;

namespace Jerrycurl.Relations
{
    public static class RelationExtensions
    {
        public static ITuple Row(this IRelation relation) => relation.Body.FirstOrDefault();
        public static IField Scalar(this IRelation relation) => relation.Body.FirstOrDefault()?.FirstOrDefault();
        public static IEnumerable<IField> Column(this IRelation relation) => relation.Body.Select(t => t.FirstOrDefault());
    }
}
