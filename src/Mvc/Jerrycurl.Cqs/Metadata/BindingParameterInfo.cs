﻿using System.Data;
using Jerrycurl.Relations;

namespace Jerrycurl.Cqs.Metadata
{
    internal class BindingParameterInfo : IBindingParameterInfo
    {
        public IBindingMetadata Metadata { get; set; }
        public IDbDataParameter Parameter { get; set; }
        public IField Field { get; set; }
    }
}
