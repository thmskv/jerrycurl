﻿namespace Jerrycurl.Cqs.Metadata
{
    public class BindingParameterContract : IBindingParameterContract
    {
        public BindingParameterWriter Write { get; set; }
        public BindingParameterConverter Convert { get; set; }
    }
}
