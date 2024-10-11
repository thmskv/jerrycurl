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
using System.Xml.Serialization;
using Microsoft.CodeAnalysis.CSharp;

namespace Jerrycurl.CodeAnalysis.Razor.Compiler;

[Generator]
public class RazorSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var options = context.ParseOptionsProvider.Select((opt, _) => opt as CSharpParseOptions);

        context.RegisterSourceOutput(options, (context, data) =>
        {
            context.AddSource("_ParseOptions", "/*" + data.Language + " " + data.LanguageVersion + " " + data.SpecifiedLanguageVersion + "*/");
        });

        var sourceItems = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".cssql", StringComparison.OrdinalIgnoreCase))
            .Select(static (file, cancellationToken) =>
            {
                var text = file.GetText(cancellationToken);

                return (
                    file.Path,
                    Content: text.ToString(),
                    Checksum: string.Join("", text.GetChecksum().Select(b => $"{b:x2}"))
                );
            })
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
                var additionalText = data.Left.Left;
                var config = data.Left.Right;
                var imports = data.Right;

                try
                {
                    Go();
                }
                catch (Exception ex)
                {
                    var guid = $"{Guid.NewGuid()}";

                    context.AddSource($"jerry+{guid}.log", "/*" + ex.ToString() + "*/");
                    context.AddSource($"jerry+{guid}.alt.log", "/*" + string.Join("\n", additionalText.Path) + "*/");
                }


                //context.AddSource(file.FileName, file.Code);

                void Go()
                {
                    config.GlobalOptions.TryGetValue("build_property.ProjectDir", out var projectDirectory);
                    config.GlobalOptions.TryGetValue("build_property.RootNamespace", out var rootNamespace);
                    config.GlobalOptions.TryGetValue("build_property.LangVersion", out var rootNamespace);

                    var project = new RazorProject()
                    {
                        ProjectDirectory = projectDirectory,
                        RootNamespace = rootNamespace,
                    };

                    foreach (var (importText, _) in imports)
                        project.AddItem(importText.Path, content: importText.Content, checksum: importText.Checksum);

                    project.AddItem(additionalText.Path, content: additionalText.Content, checksum: additionalText.Checksum);

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
        public override void Throw(Exception ex)
        {
            $throw$
        }
	}
$endnamespace$
#pragma warning restore IDE0009",
                        Imports = RazorFacts.DefaultNamespaces.Select(ns => new RazorFragment() { Text = ns }).ToList(),
                    });

                    var parsed = parser.Parse(project);

                    foreach (var razorPage in parsed)
                    {
                        var hintName = CSharp.Identifier(razorPage.ProjectPath) + ".g";
                        var projectionResult = generator.Generate(razorPage.Data);
                        var code = projectionResult.Content;

                        context.AddSource(hintName, code);
                    }
                }
            }
        );
    }
}
