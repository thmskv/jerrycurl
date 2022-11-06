﻿using System;

namespace Jerrycurl.Cqs.Metadata.Annotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class KeyAttribute : Attribute
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public bool IsPrimary { get; set; } = true;

        public KeyAttribute()
        {

        }

        public KeyAttribute(int index)
        {
            this.Index = index;
        }

        public KeyAttribute(string name)
        {
            this.Name = name;
        }

        public KeyAttribute(string name, int index)
        {
            this.Name = name;
            this.Index = index;
        }
    }
}
