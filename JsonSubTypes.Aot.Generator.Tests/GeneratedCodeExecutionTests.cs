#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;

namespace JsonSubTypes.Aot.Generator.Tests
{
    // Compiles the driver output (source + generated converters) into a real
    // assembly and runs the generated converters through reflection. This is what
    // gives coverlet visibility into the *generated* code paths.
    [TestFixture]
    public class GeneratedCodeExecutionTests
    {
        private const string Hierarchy = @"
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

        private static readonly (Type AnimalType, Type CatType, object Converter) Loaded = CompileAndLoad();

        // The converter is a static singleton (the fixture compiles and loads once);
        // the options instance is shared by every test so it is created only once.
        private static readonly JsonSerializerOptions Options = new()
        {
            Converters = { (JsonConverter)Loaded.Converter }
        };

        private static (Type animalType, Type catType, object converter) CompileAndLoad()
        {
            GeneratorRun run = GeneratorDriverRunner.GetRun(Hierarchy);
            Assert.That(run.DriverResults.Diagnostics, Is.Empty);

            using MemoryStream assemblyStream = new MemoryStream();
            using MemoryStream pdbStream = new MemoryStream();
            var emitResult = run.OutputCompilation.Emit(assemblyStream, pdbStream);
            Assert.That(emitResult.Success, Is.True,
                string.Join("\n", emitResult.Diagnostics.Select(d => d.ToString())));

            Assembly assembly = Assembly.Load(assemblyStream.ToArray());
            Type[] allTypes = assembly.GetTypes();
            Type? animalType = allTypes.FirstOrDefault(t => t.Name == "Animal");
            Type? catType = allTypes.FirstOrDefault(t => t.Name == "Cat");
            Type? converterType = allTypes.FirstOrDefault(t => t.Name == "AnimalJsonSubTypesConverter");
            Assert.That(animalType, Is.Not.Null, "types: " + string.Join(", ", allTypes.Select(t => t.FullName)));
            Assert.That(catType, Is.Not.Null);
            Assert.That(converterType, Is.Not.Null);
            object converter = Activator.CreateInstance(converterType!)!;
            return (animalType!, catType!, converter);
        }

        [Test]
        public void Serialize_Cat_WritesDiscriminator()
        {
            object cat = Activator.CreateInstance(Loaded.CatType)!;
            Loaded.CatType.GetProperty("Age")!.SetValue(cat, 3);
            Loaded.CatType.GetProperty("Lives")!.SetValue(cat, 9);

            string json = JsonSerializer.Serialize(cat, Loaded.AnimalType, Options);

            Assert.That(json, Does.Contain("\"type\":\"cat\""));
        }

        [Test]
        public void Deserialize_CatDiscriminator_ReturnsCat()
        {
            object? result = JsonSerializer.Deserialize("{\"type\":\"cat\",\"Lives\":9,\"Age\":3}", Loaded.AnimalType, Options);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetType().Name, Is.EqualTo("Cat"));
        }

        [Test]
        public void Deserialize_UnknownDiscriminator_FallsBackToBase()
        {
            object? result = JsonSerializer.Deserialize("{\"type\":\"fish\",\"Age\":3}", Loaded.AnimalType, Options);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetType().Name, Is.EqualTo("Animal"));
        }
    }
}
