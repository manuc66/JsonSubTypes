#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using JsonSubTypes.Aot;
using JsonSubTypes.Text.Json;

namespace JsonSubTypes.Aot.Generator.Tests
{
    internal static class GeneratorDriverRunner
    {
        // Reference the assemblies the generated code needs: System.Text.Json for
        // the JsonConverter base, and JsonSubTypes.Text.Json for the attributes.
        // Loading the runtime assemblies from their loaded location keeps the
        // reference set aligned with the runtime the tests execute on.
        private static readonly List<MetadataReference> References = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JsonConverterAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(JsonSubTypesAotConverterAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Private.CoreLib").Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Memory").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Collections.Concurrent").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Text.Json").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime.CompilerServices.Unsafe").Location),
        };

        public static GeneratorRun GetRun(string source)
        {
            CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, parseOptions);
            CSharpCompilation compilation = CSharpCompilation.Create(
                "GeneratorTest",
                new[] { tree },
                References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new JsonSubTypesGenerator());
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation, out _);

            return new GeneratorRun(driver.GetRunResult(), outputCompilation);
        }

        public static string? GetGeneratedSource(GeneratorRun run, string hintName)
        {
            foreach (GeneratorRunResult result in run.DriverResults.Results)
            {
                foreach (GeneratedSourceResult source in result.GeneratedSources)
                {
                    if (source.HintName == hintName)
                    {
                        return source.SourceText.ToString();
                    }
                }
            }
            return null;
        }
    }

    internal sealed class GeneratorRun
    {
        public GeneratorDriverRunResult DriverResults { get; }
        public Compilation OutputCompilation { get; }

        public GeneratorRun(GeneratorDriverRunResult driverResults, Compilation outputCompilation)
        {
            DriverResults = driverResults;
            OutputCompilation = outputCompilation;
        }
    }
}
