using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Vendors.Oracle.Test.Models;

[Table("jerry_booleans")]
public class BooleanModel
{
    public bool True { get; set; }
    public bool False { get; set; }
    public bool? Null { get; set; }
    public bool? NotNull { get; set; }
}
