using Shouldly;

namespace Jerrycurl.CodeAnalysis.Razor.Test;

public class VisibilityTests
{
    private const string Page = "@query\nSELECT 1";

    public void Test_Default_IsInternal()
    {
        GeneratorHelper.GenerateOne(Page).ShouldContain("internal partial class Page");
    }

    public void Test_PageDirective_Public()
    {
        GeneratorHelper.GenerateOne("@visibility public\n" + Page).ShouldContain("public partial class Page");
    }

    public void Test_ProjectDefault_Public()
    {
        GeneratorHelper.Generate("public", ("Queries/Page.cssql", Page))["Queries/Page.cssql"].ShouldContain("public partial class Page");
    }

    public void Test_PageDirective_OverridesProjectDefault()
    {
        GeneratorHelper.Generate("public", ("Queries/Page.cssql", "@visibility internal\n" + Page))["Queries/Page.cssql"].ShouldContain("internal partial class Page");
    }

    public void Test_Imports_AreInherited_NearestWins_PageOverrides()
    {
        var result = GeneratorHelper.Generate(
            ("Queries/_imports.cssql", "@visibility public"),
            ("Queries/Internal/_imports.cssql", "@visibility internal"),
            ("Queries/A.cssql", Page),
            ("Queries/Internal/B.cssql", Page),
            ("Queries/Internal/C.cssql", "@visibility public\n" + Page),
            ("Other/D.cssql", Page));

        result["Queries/A.cssql"].ShouldContain("public partial class A");
        result["Queries/Internal/B.cssql"].ShouldContain("internal partial class B");
        result["Queries/Internal/C.cssql"].ShouldContain("public partial class C");
        result["Other/D.cssql"].ShouldContain("internal partial class D");
    }

    public void Test_Partials_FollowSameRule()
    {
        GeneratorHelper.GenerateOne("@visibility public\nSELECT 1").ShouldContain("public partial class Page");
    }
}
