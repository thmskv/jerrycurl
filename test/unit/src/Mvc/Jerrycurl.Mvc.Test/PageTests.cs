using System.Linq;
using Jerrycurl.Mvc.Test.Project.Pages;
using Jerrycurl.Mvc.Test.Project.Pages.Public;
using Shouldly;

namespace Jerrycurl.Mvc.Test;

/// <summary>
/// Runs real .cssql pages compiled by the source generator (see Project/Pages).
/// </summary>
public class PageTests
{
    public void Test_Query_WithInputAndOutput()
    {
        var result = GetBlog.Query(new() { Id = 12, Title = "Hello" });

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(12);
        result[0].Title.ShouldBe("Hello");
    }

    public void Test_Query_InputDefaults()
    {
        GetBlog.Query(new() { Id = 1 }).Single().Title.ShouldBe("Default title");
    }

    public async Task Test_QueryAsync_And_AggregateAsync()
    {
        (await GetBlog.QueryAsync(new() { Id = 3 })).Single().Id.ShouldBe(3);
        (await GetBlog.AggregateAsync(new() { Id = 4 })).Id.ShouldBe(4);
    }

    public void Test_Query_Enumerate()
    {
        GetNumbers.Enumerate(10).ShouldBe([10, 11]);
    }

    public void Test_Query_WithModelAndResult()
    {
        GetNumbers.Query(5).ShouldBe([5, 6]);
    }

    public void Test_Command_WritesBackIntoModel()
    {
        var model = new SetResult.Input() { Id = 7 };

        SetResult.Execute(model);

        model.Result.ShouldBe("done 7");
    }

    public async Task Test_CommandAsync_WritesBackIntoModel()
    {
        var model = new SetResult.Input() { Id = 8 };

        await SetResult.ExecuteAsync(model);

        model.Result.ShouldBe("done 8");
    }

    public void Test_Partial_HasNoEntryPoints()
    {
        typeof(Columns).GetMethods().ShouldNotContain(m => m.IsStatic && (m.Name == "Query" || m.Name == "Execute"));
    }

    public void Test_Visibility()
    {
        typeof(GetBlog).IsPublic.ShouldBeFalse();
        typeof(PublicQuery).IsPublic.ShouldBeTrue();
        typeof(StillInternal).IsPublic.ShouldBeFalse();
    }
}
