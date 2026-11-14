using System;
using System.Collections.Generic;
using Jerrycurl.Cqs.Metadata.Annotations;

namespace Jerrycurl.Vendors.SqlServer.Test.Models;

[Table("dbo", "jerry_tt_dates")]
public class DateTvpModel
{
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
}

public class DateTvpContainer
{
    public IList<DateTvpModel> Items { get; set; }
}
