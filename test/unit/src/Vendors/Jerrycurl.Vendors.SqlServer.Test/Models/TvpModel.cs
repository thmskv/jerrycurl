using System;
using System.Xml.Linq;
using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Vendors.SqlServer.Test.Models
{
    [Table("dbo", "jerry_tt")]
    public class TvpModel
    {
        public bool Bool { get; set; }
        public short Int16 { get; set; }
        public int Int32 { get; set; }
        public long Int64 { get; set; }
        public float Float { get; set; }
        public double Double { get; set; }
        public decimal Decimal { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public DateTime DateTime { get; set; }
        public DateTime DateTime2 { get; set; }
        public TimeSpan Time { get; set; }
        public string String { get; set; }
        public byte[] Bytes { get; set; }
        public Guid Guid { get; set; }
    }
}
