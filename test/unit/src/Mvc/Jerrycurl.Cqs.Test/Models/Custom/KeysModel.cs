using System.Collections.Generic;
using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Cqs.Test.Models.Custom
{
    public class KeysModel
    {
        [Key("PK_1")]
        public int? Id1 { get; set; }

        [Key("PK_2", IsPrimary = false)]
        public int? Id2 { get; set; }

        public IList<RefModel1> Many1 { get; set; }
        public IList<RefModel2> Many2 { get; set; }

        public class RefModel1
        {
            [Ref("PK_1")]
            public int? KeyId { get; set; }
        }

        public class RefModel2
        {
            [Ref("PK_2")]
            public int? KeyId { get; set; }
        }
    }
}
