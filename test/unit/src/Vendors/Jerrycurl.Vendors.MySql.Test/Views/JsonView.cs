﻿using Jerrycurl.Cqs.Metadata.Annotations;
using Jerrycurl.Vendors.MySql.Test.Models;

namespace Jerrycurl.Vendors.MySql.Test.Views
{
    [Table]
    public class JsonView
    {
        public JsonModel Json { get; set; }
        public string JsonString { get; set; }
    }
}
