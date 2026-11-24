using System;
using System.Linq;
using Jerrycurl.Mvc.Test.Project.Queries.Misc;
using Shouldly;

namespace Jerrycurl.Mvc.Test;

public class EntryPointTests
{
    public void Test_Query_RunsWithoutAccessor()
    {
        EntryPointQuery.Query().ShouldBe([1, 2]);
    }

    public async Task Test_QueryAsync_RunsWithoutAccessor()
    {
        (await EntryPointQuery.QueryAsync()).ShouldBe([1, 2]);
    }

    public void Test_Aggregate_MergesRows()
    {
        // Aggregating into a scalar keeps the last row's value (Cqs aggregate semantics).
        EntryPointQuery.Aggregate().ShouldBe(2);
    }

    public void Test_Enumerate_RunsWithoutAccessor()
    {
        EntryPointQuery.Enumerate().ToList().ShouldBe([1, 2]);
    }

    public void Test_Query_UsesExplicitEngine()
    {
        EntryPointQuery.Query(new ProcEngine(null)).ShouldBe([1, 2]);
    }

    public void Test_Describe_NonPage_Throws()
    {
        Should.Throw<ProcExecutionException>(() => new ProcEngine(null).Describe(typeof(string)));
    }

    public void Test_Describe_FindsDomain()
    {
        new ProcEngine(null).Describe(typeof(EntryPointQuery)).DomainType.ShouldBe(typeof(Project.TestDomain));
    }
}
