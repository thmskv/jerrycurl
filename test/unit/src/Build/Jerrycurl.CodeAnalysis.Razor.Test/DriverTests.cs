using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Shouldly;

namespace Jerrycurl.CodeAnalysis.Razor.Test;

public class DriverTests
{
    private static string[] Errors(GeneratorDriverRunResult result) => [.. result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Id)];

    public void Test_Driver_GeneratesSource()
    {
        var result = DriverHelper.Run(("Queries/Get.cssql", "@query\n@input int Id\nSELECT 1"));

        Errors(result).ShouldBeEmpty();
        result.GeneratedTrees.Single().ToString().ShouldContain("internal partial class Get");
    }

    public void Test_Driver_ReportsDiagnosticAtCssqlLocation()
    {
        var result = DriverHelper.Run(("Queries/Get.cssql", "SELECT 1\n@query\n@command"));

        Diagnostic error = result.Diagnostics.Single(d => d.Severity == DiagnosticSeverity.Error);

        error.Id.ShouldBe("JC0001");
        error.Location.GetLineSpan().Path.ShouldEndWith("Get.cssql");
        error.Location.GetLineSpan().StartLinePosition.Line.ShouldBe(2);
    }

    public void Test_Driver_KindInImports_IsReportedOnce()
    {
        var result = DriverHelper.Run(
            ("Queries/_imports.cssql", "@query\n@visibility public"),
            ("Queries/A.cssql", "SELECT 1"),
            ("Queries/B.cssql", "SELECT 2"));

        Diagnostic error = result.Diagnostics.Single(d => d.Severity == DiagnosticSeverity.Error);

        error.Id.ShouldBe("JC0003");
        error.Location.GetLineSpan().Path.ShouldEndWith("_imports.cssql");
    }

    public void Test_Driver_InvalidVisibilityInImports_IsReportedOnce()
    {
        var result = DriverHelper.Run(
            ("Queries/_imports.cssql", "@visibility protected"),
            ("Queries/A.cssql", "@query\nSELECT 1"),
            ("Queries/B.cssql", "@query\nSELECT 2"));

        Errors(result).ShouldBe(["JC0005"]);
        result.GeneratedTrees.ShouldAllBe(t => t.ToString().Contains("internal partial class"));
    }

    public void Test_Driver_MsBuildVisibility()
    {
        var result = DriverHelper.Run(new Dictionary<string, string>() { ["JerrycurlPageVisibility"] = "public" }, ("Queries/Get.cssql", "@query\nSELECT 1"));

        result.GeneratedTrees.Single().ToString().ShouldContain("public partial class Get");
    }

    public void Test_Driver_InvalidMsBuildVisibility()
    {
        var result = DriverHelper.Run(new Dictionary<string, string>() { ["JerrycurlPageVisibility"] = "secret" }, ("Queries/Get.cssql", "@query\nSELECT 1"));

        Errors(result).ShouldBe(["JC0005"]);
        result.GeneratedTrees.Single().ToString().ShouldContain("internal partial class Get");
    }
}
