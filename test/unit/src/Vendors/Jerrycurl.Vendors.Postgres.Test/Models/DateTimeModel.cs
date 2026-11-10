using System;
using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Vendors.Postgres.Test.Models;

[Table("jerry_dateonly")]
public class DateTimeModel
{
    public DateOnly Date { get; set; }
    public DateOnly? NullDate { get; set; }
    public DateOnly? NotNullDate { get; set; }
    public TimeOnly Time { get; set; }
    public TimeOnly? NullTime { get; set; }
    public TimeOnly? NotNullTime { get; set; }
}
