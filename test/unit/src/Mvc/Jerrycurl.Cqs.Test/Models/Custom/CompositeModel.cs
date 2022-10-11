using System;
using System.Collections.Generic;
using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Cqs.Test.Models.Custom
{
    public class CompositeModel
    {
        [Key("K", Index = 1)]
        public string Key1 { get; set; }
        [Key("K", Index = 2)]
        public Guid Key2 { get; set; }
        [Key("K", Index = 3)]
        public int Key3 { get; set; }

        public IList<RefModel> Refs { get; set; }

        public class RefModel
        {
            [Ref("K", Index = 3)]
            public int Ref3 { get; set; }
            [Ref("K", Index = 2)]
            public Guid Ref2 { get; set; }
            [Ref("K", Index = 1)]
            public string Ref1 { get; set; }
        }
    }
}
