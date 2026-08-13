#nullable enable
using System.Linq;
using NUnit.Framework;

namespace JsonSubTypes.Text.Json.Aot.Generator.Tests
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

        [Test]
        public void Generate_BaseProperties_HonorIgnoreConditionsAndGetterOnly()
        {
            const string domain = @"
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;

[JsonSubTypesAotConverter(""kind"")]
[KnownSubType(typeof(Sub), ""sub"")]
public class Base
{
    public int Age { get; set; }
    public string Computed { get { return ""x""; } }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Nickname { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Serial { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string? Note { get; set; }
    [JsonIgnore]
    public string? Secret { get; set; }
}
public class Sub : Base { }
";
            GeneratorRun run = GeneratorDriverRunner.GetRun(domain);

            string? text = GeneratorDriverRunner.GetGeneratedSource(run, "BaseJsonSubTypesConverter.g.cs");
            Assert.That(text, Is.Not.Null);
            StringAssert.Contains("value.Computed", text!, "get-only property must be written");
            StringAssert.Contains("value.Nickname != null", text!, "WhenWritingNull must guard the write");
            StringAssert.Contains("EqualityComparer<int>.Default.Equals(value.Serial, default)", text!, "WhenWritingDefault must guard the write");
            StringAssert.Contains("value.Note", text!, "Never must write the property");
            StringAssert.DoesNotContain("value.Secret", text!, "Always must drop the property");
        }

        [Test]
        public void Generate_ExplicitAlwaysIgnore_DropsProperty()
        {
            const string domain = @"
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;

[JsonSubTypesAotConverter(""kind"")]
[KnownSubType(typeof(Sub), ""sub"")]
public class Base
{
    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string? Secret { get; set; }
}
public class Sub : Base { }
";
            GeneratorRun run = GeneratorDriverRunner.GetRun(domain);

            string? text = GeneratorDriverRunner.GetGeneratedSource(run, "BaseJsonSubTypesConverter.g.cs");
            Assert.That(text, Is.Not.Null);
            StringAssert.DoesNotContain("value.Secret", text!);
        }

        [Test]
        public void Generate_JsonPropertyName_EmitsCustomName()
        {
            const string domain = @"
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;

[JsonSubTypesAotConverter(""kind"")]
[KnownSubType(typeof(Sub), ""sub"")]
public class Base
{
    [JsonPropertyName(""age"")]
    public int Age { get; set; }
}
public class Sub : Base { }
";
            GeneratorRun run = GeneratorDriverRunner.GetRun(domain);

            string? text = GeneratorDriverRunner.GetGeneratedSource(run, "BaseJsonSubTypesConverter.g.cs");
            Assert.That(text, Is.Not.Null);
            StringAssert.Contains("= \"age\";", text!);
        }

        [Test]
        public void Generate_UnsupportedDiscriminator_ReportsJSTAOT001()
        {
            const string domain = @"
using JsonSubTypes.Text.Json;

[JsonSubTypesAotConverter(""type"")]
[KnownSubType(typeof(Sub), 1.5)]
public class Base { }
public class Sub : Base { }
";
            GeneratorRun run = GeneratorDriverRunner.GetRun(domain);

            Assert.That(run.DriverResults.Diagnostics.Select(d => d.Id), Does.Contain("JSTAOT001"));
        }

        [Test]
        public void Generate_BoolDiscriminator_ReportsJSTAOT001()
        {
            const string domain = @"
using JsonSubTypes.Text.Json;

[JsonSubTypesAotConverter(""type"")]
[KnownSubType(typeof(Sub), true)]
public class Base { }
public class Sub : Base { }
";
            GeneratorRun run = GeneratorDriverRunner.GetRun(domain);

            Assert.That(run.DriverResults.Diagnostics.Select(d => d.Id), Does.Contain("JSTAOT001"));
        }

        [Test]
        public void Generate_EnumWithoutMatchingMember_ReportsJSTAOT001()
        {
            const string domain = @"
using JsonSubTypes.Text.Json;

public enum Kind { A, B }

[JsonSubTypesAotConverter(""type"")]
[KnownSubType(typeof(Sub), (Kind)99)]
public class Base { }
public class Sub : Base { }
";
            GeneratorRun run = GeneratorDriverRunner.GetRun(domain);

            Assert.That(run.DriverResults.Diagnostics.Select(d => d.Id), Does.Contain("JSTAOT001"));
        }

        [Test]
        public void Generate_DuplicateDiscriminators_ReportsJSTAOT002()
        {
            const string domain = @"
#pragma warning disable JSTAOT002
using JsonSubTypes.Text.Json;

[JsonSubTypesAotConverter(""type"")]
[KnownSubType(typeof(Sub), ""a"")]
[KnownSubType(typeof(Sub), ""b"")]
public class Base { }
public class Sub : Base { }
#pragma warning restore JSTAOT002
";
            GeneratorRun run = GeneratorDriverRunner.GetRun(domain);

            Assert.That(run.DriverResults.Diagnostics.Select(d => d.Id), Does.Contain("JSTAOT002"));
        }

        [Test]
        public void Generate_PresenceModeValueRegistration_ReportsJSTAOT003()
        {
            const string domain = @"
using JsonSubTypes.Text.Json;

[JsonSubTypesAotConverter]
[KnownSubType(typeof(Sub), ""a"")]
[KnownSubTypeWithProperty(typeof(Sub), ""Marker"")]
public class Base { }
public class Sub : Base { public int Marker { get; set; } }
";
            GeneratorRun run = GeneratorDriverRunner.GetRun(domain);

            Assert.That(run.DriverResults.Diagnostics.Select(d => d.Id), Does.Contain("JSTAOT003"));
        }

        [Test]
        public void Generate_AbstractBase_EmitsThrowingDeserializeBase()
        {
            const string domain = @"
using JsonSubTypes.Text.Json;

[JsonSubTypesAotConverter(""type"")]
[KnownSubType(typeof(Sub), ""sub"")]
public abstract class Base { }
public class Sub : Base { }
";
            GeneratorRun run = GeneratorDriverRunner.GetRun(domain);

            string? text = GeneratorDriverRunner.GetGeneratedSource(run, "BaseJsonSubTypesConverter.g.cs");
            Assert.That(text, Is.Not.Null);
            StringAssert.Contains("interface or abstract class", text!);
        }

        [Test]
        public void Generate_ParameterizedBase_EmitsRequiringParameterlessCtor()
        {
            const string domain = @"
using JsonSubTypes.Text.Json;

[JsonSubTypesAotConverter(""type"")]
[KnownSubType(typeof(Sub), ""sub"")]
public class Base
{
    public Base(string name) { }
}
public class Sub : Base
{
    public Sub() : base(""x"") { }
}
";
            GeneratorRun run = GeneratorDriverRunner.GetRun(domain);

            string? text = GeneratorDriverRunner.GetGeneratedSource(run, "BaseJsonSubTypesConverter.g.cs");
            Assert.That(text, Is.Not.Null);
            StringAssert.Contains("parameterless constructor", text!);
        }
    }
}
