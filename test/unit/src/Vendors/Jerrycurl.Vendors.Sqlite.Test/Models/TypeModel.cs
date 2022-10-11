﻿using System.Text;
using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Vendors.Sqlite.Test.Models
{
    [Table]
    public class TypeModel
    {
        public int Integer { get; set; }
        public double Real { get; set; }
        public string Text { get; set; }
        public byte[] Blob { get; set; }

        public static TypeModel GetSample()
        {
            return new TypeModel()
            {
                Integer = 100000,
                Real = 3212.124d,
                Text = "Jerrycurl",
                Blob = Encoding.ASCII.GetBytes("Jerrycurl"),
            };
        }
    }
}
