using System;
using Jerrycurl.CodeAnalysis.Projection;
using Jerrycurl.CodeAnalysis.Razor.Generation;
using Jerrycurl.CodeAnalysis.Razor.Lexing.CSharp;
using Jerrycurl.CodeAnalysis.Razor.Parsing;
using Jerrycurl.CodeAnalysis.Razor.ProjectSystem;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Jerrycurl.IO;
using System.IO;
using Jerrycurl.CodeAnalysis;
using Jerrycurl.Facts;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Net;

namespace Jerrycurl.SourceGenerator.Razor;

[Generator]
public class RazorGeneratorX : IIncrementalGenerator
{
    private const string SourceItemGroupMetadata = "build_metadata.AdditionalFiles.SourceItemGroup";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sourceItems = context.AdditionalTextsProvider
            .Where(static (file) => file.Path.EndsWith(".cssql", StringComparison.OrdinalIgnoreCase))
            .Combine(context.AnalyzerConfigOptionsProvider);

        var importFiles = sourceItems.Where(static file =>
        {
            var path = file.Left.Path;

            if (Path.GetFileNameWithoutExtension(path) == "_imports")
                return true;

            return false;
        });

        var finalItems = sourceItems.Combine(importFiles.Collect());


        context.RegisterSourceOutput(
            finalItems,
            (context, data) =>
            {
                //Debugger.Launch();

                var additionalText = data.Left.Left;
                var config = data.Left.Right;
                var imports = data.Right;

                try
                {
                    Go();
                }
                catch (Exception ex)
                {
                    context.AddSource("jerry.log", "/*" + ex.ToString() + "*/");
                    context.AddSource("jerry2.log", "/*" + string.Join("\n", additionalText.Path) + "*/");
                }


                //context.AddSource(file.FileName, file.Code);

                void Go()
                {


                    config.GlobalOptions.TryGetValue("build_property.ProjectDir", out var projectDirectory);
                    config.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);

                    var project = new RazorProject()
                    {
                        ProjectDirectory = projectDirectory.TrimEnd('\\'),
                        RootNamespace = rootNamespace,
                    };

                    foreach (var (additionalFile, _) in imports)
                        project.AddItem(additionalFile.Path);

                    project.AddItem(additionalText.Path);

                    var buf = new List<string>() { Guid.NewGuid().ToString() };
                    foreach (var it in project.Items)
                        buf.Add(it.FullPath + " / " + it.ProjectPath);

                    //context.AddSource("jerry3.log", "/*" + string.Join("\n", buf) + "*/");


                    var parser = new RazorParser();
                    var generator = new RazorGenerator(new RazorGeneratorOptions()
                    {
                        TemplateCode = @"$pragmachecksum$
#pragma warning disable IDE0009
$globalimports$

$beginnamespace$
	#line hidden
	$localimports$
	$template$
	internal class $class$ : global::Jerrycurl.Mvc.ProcPage<$model$,$result$>
	{
		#pragma warning disable IDE1006
		$injectiondefs$
		#pragma warning restore IDE1006
    
		public $class$(global::Jerrycurl.Mvc.Projections.IProjection model, global::Jerrycurl.Mvc.Projections.IProjection result)
			: base(model, result)
		{
			
		}
		public override void Execute()
		{
			$execute$
		}
	}
$endnamespace$
#pragma warning restore IDE0009",
                        Imports = RazorFacts.DefaultNamespaces.Select(ns => new RazorFragment() { Text = ns }).ToList(),
                    });

                    var parsed = parser.Parse(project);

                    foreach (var razorPage in parsed)
                    {
                        var hintName = CSharp.Identifier(razorPage.ProjectPath) + ".g.cssql";
                        var projectionResult = generator.Generate(razorPage.Data);

                        context.AddSource(hintName, projectionResult.Content);
                    }
                }
            }
        );
    }
}
