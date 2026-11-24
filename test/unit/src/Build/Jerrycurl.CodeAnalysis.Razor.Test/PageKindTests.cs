using System.Linq;
using Jerrycurl.CodeAnalysis.Razor.Parsing;
using Shouldly;

namespace Jerrycurl.CodeAnalysis.Razor.Test;

public class PageKindTests
{
    public void Test_Query_GeneratesStaticEntryPoints()
    {
        string code = GeneratorHelper.GenerateOne("@query\n@input int Id\n@output int Id, string Title\nSELECT 1", "Queries/Blogs/GetAll.cssql");

        code.ShouldContain("namespace TestApp.Queries.Blogs");
        code.ShouldContain("internal partial class GetAll : global::Jerrycurl.Mvc.ProcPage<GetAll.Input,GetAll.Output>");
        code.ShouldContain("public static global::System.Collections.Generic.IList<Output> Query(Input model = default,");
        code.ShouldContain("public static global::System.Collections.Generic.IList<T> Query<T>(Input model = default,");
        code.ShouldContain("QueryAsync(");
        code.ShouldContain("Aggregate(");
        code.ShouldContain("AggregateAsync(");
        code.ShouldContain("Enumerate(");
        code.ShouldContain("EnumerateAsync(");
        code.ShouldNotContain(" Execute(");

        GeneratorHelper.Compile(code).ShouldBeEmpty();
    }

    public void Test_Query_WithoutInputOrOutput_UsesObject()
    {
        string code = GeneratorHelper.GenerateOne("@query\nSELECT 1");

        code.ShouldContain("IList<object> Query(object model = default,");
        GeneratorHelper.Compile(code).ShouldBeEmpty();
    }

    public void Test_Query_WithModelAndResult_UsesThem()
    {
        string code = GeneratorHelper.GenerateOne("@query\n@model int\n@result string\nSELECT 1");

        code.ShouldContain("IList<string> Query(int model = default,");
        GeneratorHelper.Compile(code).ShouldBeEmpty();
    }

    public void Test_Command_GeneratesExecute()
    {
        string code = GeneratorHelper.GenerateOne("@command\n@input int Id\nUPDATE x", "Commands/Touch.cssql");

        code.ShouldContain("public static void Execute(Input model = default,");
        code.ShouldContain("public static global::System.Threading.Tasks.Task ExecuteAsync(Input model = default,");
        code.ShouldNotContain(" Query(");

        GeneratorHelper.Compile(code).ShouldBeEmpty();
    }

    public void Test_Partial_GeneratesNoEntryPoints()
    {
        string code = GeneratorHelper.GenerateOne("SELECT 1", "Queries/Shared/Columns.cssql");

        code.ShouldContain("internal partial class Columns");
        code.ShouldNotContain(" Query(");
        code.ShouldNotContain(" Execute(");

        GeneratorHelper.Compile(code).ShouldBeEmpty();
        GeneratorHelper.Diagnose("SELECT 1").Select(d => (d.Id, d.Severity)).ShouldBe([(RazorDiagnostics.PartialPage, RazorDiagnosticSeverity.Info)]);
    }

    public void Test_UserPartial_CanAddMembers()
    {
        string code = GeneratorHelper.GenerateOne("@query\n@input int Id\nSELECT 1", "Queries/Get.cssql");
        string user = "namespace TestApp.Queries { partial class Get { public static int Twice(int x) => x * 2; } }";

        GeneratorHelper.Compile(code, user).ShouldBeEmpty();
    }

    public void Test_Diagnostics_BothKinds()
    {
        GeneratorHelper.Diagnose("@query\n@command\nSELECT 1").Select(d => d.Id).ShouldBe([RazorDiagnostics.BothKinds]);
    }

    public void Test_Diagnostics_OutputOnCommand()
    {
        GeneratorHelper.Diagnose("@command\n@output int Id\nSELECT 1").Select(d => d.Id).ShouldBe([RazorDiagnostics.OutputOnCommand]);
        GeneratorHelper.Diagnose("@command\n@result int\nSELECT 1").Select(d => d.Id).ShouldBe([RazorDiagnostics.OutputOnCommand]);
    }

    public void Test_Diagnostics_InvalidSyntax()
    {
        GeneratorHelper.Diagnose("@query foo\nSELECT 1").Select(d => d.Id).ShouldBe([RazorDiagnostics.InvalidSyntax]);
        GeneratorHelper.Diagnose("@query\n@input Id\nSELECT 1").Select(d => d.Id).ShouldBe([RazorDiagnostics.InvalidSyntax]);
        GeneratorHelper.Diagnose("@query\n@output int Id, Title\nSELECT 1").Select(d => d.Id).ShouldBe([RazorDiagnostics.InvalidSyntax]);
    }

    public void Test_Diagnostics_InvalidVisibility()
    {
        GeneratorHelper.Diagnose("@query\n@visibility private\nSELECT 1").Select(d => d.Id).ShouldBe([RazorDiagnostics.InvalidVisibility]);
    }

    public void Test_Diagnostics_NameClash()
    {
        GeneratorHelper.Diagnose("@query\nSELECT 1", "Queries/Query.cssql").Select(d => d.Id).ShouldBe([RazorDiagnostics.NameClash]);
        GeneratorHelper.Diagnose("@command\nSELECT 1", "Commands/Execute.cssql").Select(d => d.Id).ShouldBe([RazorDiagnostics.NameClash]);
        GeneratorHelper.Diagnose("@command\nSELECT 1", "Commands/Query.cssql").ShouldBeEmpty();
    }

    public void Test_Diagnostics_HaveLineNumbers()
    {
        RazorDiagnostic diagnostic = GeneratorHelper.Diagnose("SELECT 1\n@query\n@command\nSELECT 2").Single();

        diagnostic.Span.ShouldNotBeNull();
        diagnostic.Span.Value.Line.ShouldBe(3);
    }
}
