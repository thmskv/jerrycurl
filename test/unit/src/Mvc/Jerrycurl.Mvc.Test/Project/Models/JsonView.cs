using System.Collections.Generic;
using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Mvc.Test.Project.Models
{
    public class JsonView
    {
        public JsonModel Json { get; set; }

        [Json]
        public class JsonModel
        {
            public int Value { get; set; }
            public ValueModel Model { get; set; }
            public IList<ValueModel> List { get; set; }
        }

        public class ValueModel
        {
            public int Value { get; set; }
        }
    }
}
