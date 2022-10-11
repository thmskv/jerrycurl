using System.Collections.Generic;
using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Cqs.Test.Models.Custom
{
    public class NaturalModel
    {
        [Key]
        public int NaturalId { get; set; }
        public List<InnerModel> Many { get; set; }

        public class InnerModel
        {
            [Ref]
            public int NaturalId { get; set; }
        }
    }
}
