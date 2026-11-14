using System;
using System.Collections.Generic;
using Jerrycurl.Cqs.Commands;
using Jerrycurl.Cqs.Language;
using Jerrycurl.Cqs.Queries;
using Jerrycurl.Relations.Language;
using Jerrycurl.Test;
using Shouldly;

namespace Jerrycurl.Cqs.Test;

public class DateTimeConversionTests
{
    public class ClassicModel
    {
        public DateTime Date { get; set; }
        public DateTime? NullableDate { get; set; }
        public TimeSpan Time { get; set; }
        public TimeSpan? NullableTime { get; set; }
    }

    public class ModernModel
    {
        public DateOnly Date { get; set; }
        public DateOnly? NullableDate { get; set; }
        public TimeOnly Time { get; set; }
        public TimeOnly? NullableTime { get; set; }
    }

    private static readonly string[] Columns = ["Date", "NullableDate", "Time", "NullableTime"];

    private static ClassicModel GetClassic() => new ClassicModel()
    {
        Date = new DateTime(2024, 2, 29),
        NullableDate = new DateTime(1819, 5, 4),
        Time = new TimeSpan(0, 23, 59, 59, 999),
        NullableTime = null,
    };

    private static ModernModel GetModern() => new ModernModel()
    {
        Date = new DateOnly(2024, 2, 29),
        NullableDate = new DateOnly(1819, 5, 4),
        Time = new TimeOnly(23, 59, 59, 999),
        NullableTime = null,
    };

    public void Test_Query_DateTime_To_DateOnly_TimeOnly()
    {
        var store = DatabaseHelper.Default.Store;
        var buffer = new QueryBuffer(store.GetSchema(typeof(IList<ModernModel>)), QueryType.List);

        buffer.Insert(GetClassic(), ("Date", "Item.Date"), ("NullableDate", "Item.NullableDate"), ("Time", "Item.Time"), ("NullableTime", "Item.NullableTime"));

        var result = buffer.Commit<IList<ModernModel>>();

        result.Count.ShouldBe(1);
        result[0].ShouldBeEquivalentTo(GetModern());
    }

    public void Test_Query_DateOnly_TimeOnly_To_DateTime()
    {
        var store = DatabaseHelper.Default.Store;
        var buffer = new QueryBuffer(store.GetSchema(typeof(IList<ClassicModel>)), QueryType.List);

        buffer.Insert(GetModern(), ("Date", "Item.Date"), ("NullableDate", "Item.NullableDate"), ("Time", "Item.Time"), ("NullableTime", "Item.NullableTime"));

        var result = buffer.Commit<IList<ClassicModel>>();

        result.Count.ShouldBe(1);
        result[0].ShouldBeEquivalentTo(GetClassic());
    }

    public void Test_Command_DateTime_To_DateOnly_TimeOnly()
    {
        var store = DatabaseHelper.Default.Store;
        var target = new ModernModel() { NullableTime = new TimeOnly(1, 2, 3) };
        var targets = store.From(target).Lookup(Columns);

        using var source = store.From(GetClassic()).Select(Columns).As("C1", "C2", "C3", "C4");

        var buffer = new CommandBuffer(store);

        for (int i = 0; i < Columns.Length; i++)
            buffer.Add(new ColumnBinding(targets[i], $"C{i + 1}"));

        buffer.Update(source);
        buffer.Commit();

        target.ShouldBeEquivalentTo(GetModern());
    }

    public void Test_Command_DateOnly_TimeOnly_To_DateTime()
    {
        var store = DatabaseHelper.Default.Store;
        var target = new ClassicModel() { NullableTime = new TimeSpan(1, 2, 3) };
        var targets = store.From(target).Lookup(Columns);

        using var source = store.From(GetModern()).Select(Columns).As("C1", "C2", "C3", "C4");

        var buffer = new CommandBuffer(store);

        for (int i = 0; i < Columns.Length; i++)
            buffer.Add(new ColumnBinding(targets[i], $"C{i + 1}"));

        buffer.Update(source);
        buffer.Commit();

        target.ShouldBeEquivalentTo(GetClassic());
    }
}
