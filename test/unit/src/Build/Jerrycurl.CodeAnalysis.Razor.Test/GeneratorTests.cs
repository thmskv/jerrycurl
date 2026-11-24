using Shouldly;

namespace Jerrycurl.CodeAnalysis.Razor.Test;

public class GeneratorTests
{
    public void Test_Input_DefaultValues_AreInitializers()
    {
        string code = GeneratorHelper.GenerateOne("@input int Id = 5, string Name = \"a, b\"\nSELECT 1");

        code.ShouldContain("public int Id { get; set; } = 5;");
        code.ShouldContain("public string Name { get; set; } = \"a, b\";");
        GeneratorHelper.Compile(code).ShouldBeEmpty();
    }

    public void Test_Input_GenericArguments_AreNotSplit()
    {
        string code = GeneratorHelper.GenerateOne("@input Dictionary<int, string> Map, List<(int, int)> Pairs = null\nSELECT 1");

        code.ShouldContain("public Dictionary<int, string> Map { get; set; }");
        code.ShouldContain("public List<(int, int)> Pairs { get; set; } = null;");
        GeneratorHelper.Compile(code).ShouldBeEmpty();
    }

    public void Test_Input_DefaultWithComparison_DoesNotSwallowNextArgument()
    {
        string code = GeneratorHelper.GenerateOne("@input bool Flag = 1 < 2, int Next\nSELECT 1");

        code.ShouldContain("public bool Flag { get; set; } = 1 < 2;");
        code.ShouldContain("public int Next { get; set; }");
        GeneratorHelper.Compile(code).ShouldBeEmpty();
    }

    public void Test_Output_WithoutInput_SetsResultType()
    {
        string code = GeneratorHelper.GenerateOne("@output int Id, string Title\nSELECT 1");

        code.ShouldContain("ProcPage<dynamic,Page.Output>");
        GeneratorHelper.Compile(code).ShouldBeEmpty();
    }

    public void Test_Input_WithoutOutput_ResultIsObject()
    {
        string code = GeneratorHelper.GenerateOne("@input int Id\nSELECT 1");

        code.ShouldContain("ProcPage<Page.Input,object>");
        GeneratorHelper.Compile(code).ShouldBeEmpty();
    }
}
