#nullable enable
using System.Linq;
using NUnit.Framework;

namespace JsonSubTypes.Aot.Generator.Tests
{
    [TestFixture]
    public class GeneratorDriverTests
    {
        private const string SimpleHierarchy = @"
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;

[JsonSubTypesAotConverter(""type"")]
[KnownSubType(typeof(Cat), ""cat"")]
[KnownSubType(typeof(Dog), ""dog"")]
public class Animal
{
    public int Age { get; set; }
}

public class Cat : Animal { public int Lives { get; set; } }
public class Dog : Animal { public bool CanHunt { get; set; } }
";

        private static readonly string[] SingleBaseExpectedHints =
        {
            "AnimalJsonSubTypesConverter.g.cs", "JsonSubTypesAotConverterBases.g.cs", "JsonSubTypesAotConverters.g.cs"
        };

        [Test]
        public void Generate_SingleBase_EmitsConverterAndRegistry()
        {
            GeneratorRun run = GeneratorDriverRunner.GetRun(SimpleHierarchy);

            string[] hints = run.DriverResults.Results
                .SelectMany(r => r.GeneratedSources)
                .Select(s => s.HintName).OrderBy(h => h).ToArray();
            CollectionAssert.AreEqual(SingleBaseExpectedHints, hints);
        }

        [Test]
        public void Generate_SingleBase_RegistryExposesConverterInstance()
        {
            GeneratorRun run = GeneratorDriverRunner.GetRun(SimpleHierarchy);

            string? text = GeneratorDriverRunner.GetGeneratedSource(run, "JsonSubTypesAotConverters.g.cs");
            Assert.That(text, Is.Not.Null);
            StringAssert.Contains("public static readonly AnimalJsonSubTypesConverter Animal", text!);
        }

        [Test]
        public void Generate_SingleBase_ConverterRoutesSubtypes()
        {
            GeneratorRun run = GeneratorDriverRunner.GetRun(SimpleHierarchy);

            string? text = GeneratorDriverRunner.GetGeneratedSource(run, "AnimalJsonSubTypesConverter.g.cs");
            Assert.That(text, Is.Not.Null);
            StringAssert.Contains("class AnimalJsonSubTypesConverter : JsonSubTypesAotValueConverterBase<", text!);
            StringAssert.Contains("\"cat\"", text!);
            StringAssert.Contains("\"dog\"", text!);
        }

        [Test]
        public void Generate_NoAttribute_EmitsNothing()
        {
            GeneratorRun run = GeneratorDriverRunner.GetRun("public class Plain { public int X { get; set; } }");

            Assert.That(run.DriverResults.Results.SelectMany(r => r.GeneratedSources), Is.Empty);
        }

        [Test]
        public void Generate_PropertyPresence_EmitsPresenceRouting()
        {
            const string presenceHierarchy = @"
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;

[JsonSubTypesAotConverter]
[KnownSubTypeWithProperty(typeof(Employee), ""JobTitle"")]
[KnownSubTypeWithProperty(typeof(Artist), ""Skill"")]
public class Person { public string? FirstName { get; set; } }

public class Employee : Person { public string? JobTitle { get; set; } }
public class Artist : Person { public string? Skill { get; set; } }
";
            GeneratorRun run = GeneratorDriverRunner.GetRun(presenceHierarchy);

            string? text = GeneratorDriverRunner.GetGeneratedSource(run, "PersonJsonSubTypesConverter.g.cs");
            Assert.That(text, Is.Not.Null);
            StringAssert.Contains("TryGetProperty(\"JobTitle\"", text!);
            StringAssert.Contains("TryGetProperty(\"Skill\"", text!);
        }

        [Test]
        public void Generate_TwoBasesWithSameNameInDifferentNamespaces_ProducesDistinctConverters()
        {
            const string twoAnimals = @"
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;

namespace A
{
    [JsonSubTypesAotConverter(""type"")]
    [KnownSubType(typeof(Cat), ""cat"")]
    public class Animal { public int Age { get; set; } }
    public class Cat : Animal { }
}

namespace B
{
    [JsonSubTypesAotConverter(""type"")]
    [KnownSubType(typeof(Dog), ""dog"")]
    public class Animal { public int Age { get; set; } }
    public class Dog : Animal { }
}
";
            GeneratorRun run = GeneratorDriverRunner.GetRun(twoAnimals);

            string[] hints = run.DriverResults.Results
                .SelectMany(r => r.GeneratedSources)
                .Select(s => s.HintName).OrderBy(h => h).ToArray();

            // Both converters are emitted under distinct qualified names, not one
            // overwriting the other.
            Assert.That(hints, Does.Contain("global__A_AnimalJsonSubTypesConverter.g.cs"));
            Assert.That(hints, Does.Contain("global__B_AnimalJsonSubTypesConverter.g.cs"));
            Assert.That(hints.Count(h => h.StartsWith("Animal")), Is.EqualTo(0),
                "unqualified Animal converter should not exist when names collide");
        }
    }
}
