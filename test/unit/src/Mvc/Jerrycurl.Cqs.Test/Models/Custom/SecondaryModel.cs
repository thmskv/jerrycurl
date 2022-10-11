using System.Collections.Generic;
using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Cqs.Test.Models.Custom
{
    public class SecondaryModel
    {
        [Key("PK_Secondary", IsPrimary = false)]
        public int Id { get; set; }
        public string Title { get; set; }

        public IList<RefModel> Many { get; set; }

        public class RefModel
        {
            [Ref("PK_Secondary")]
            public int RefId { get; set; }
            public string Headline { get; set; }
        }
    }
}
