#nullable enable
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace JsonSubTypes.Aot.Generator.Tests
{
    // Golden-master test: runs the generator over the committed test domain and
    // compares every produced file against the committed .g.cs files. Any change
    // in the generator's output breaks this test, forcing a conscious regeneration.
    [TestFixture]
    public class GoldenMasterTests
    {
        private const string DomainPath = "JsonSubTypes.Aot.Generated/TestDomain.cs";
        private const string GeneratedDir = "JsonSubTypes.Aot.Generated/Generated";

        private static string FindRepoRoot()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "JsonSubTypes.sln")))
            {
                dir = dir.Parent;
            }
            return dir?.FullName ?? throw new DirectoryNotFoundException("Repo root not found");
        }

        [Test]
        public void GeneratedFiles_MatchCurrentGeneratorOutput()
        {
            string root = FindRepoRoot();
            string domain = File.ReadAllText(Path.Combine(root, DomainPath));
            string generatedDir = Path.Combine(root, GeneratedDir);

            GeneratorRun run = GeneratorDriverRunner.GetRun(domain);

            var produced = run.DriverResults.Results
                .SelectMany(r => r.GeneratedSources)
                .ToDictionary(s => s.HintName, s => s.SourceText.ToString());

            // Every committed .g.cs must exist in the current output and match exactly.
            string[] committed = Directory.GetFiles(generatedDir, "*.g.cs", SearchOption.AllDirectories);
            Assert.That(produced.Count, Is.EqualTo(committed.Length),
                "Produced " + produced.Count + " files but " + committed.Length + " are committed");

            foreach (string committedFile in committed)
            {
                string hintName = Path.GetFileName(committedFile);
                string committedText = File.ReadAllText(committedFile).Trim();
                Assert.That(produced.ContainsKey(hintName), Is.True,
                    "Generator no longer produces committed file " + hintName);

                string producedText = produced[hintName].Trim();
                Assert.That(producedText, Is.EqualTo(committedText),
                    "Generator output differs from committed " + hintName +
                    ".\nRegenerate with: dotnet build JsonSubTypes.Aot.Generated -p:EmitCompilerGeneratedFiles=true");
            }

            // No extra files produced that are not committed.
            foreach (string hintName in produced.Keys)
            {
                Assert.That(committed.Any(f => Path.GetFileName(f) == hintName), Is.True,
                    "Generator produces " + hintName + " but it is not committed");
            }
        }
    }
}
