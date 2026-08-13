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
            (Type animalType, Type catType, object converter) = CompileAndLoad();
            object cat = Activator.CreateInstance(catType)!;
            catType.GetProperty("Age")!.SetValue(cat, 3);
            catType.GetProperty("Lives")!.SetValue(cat, 9);

            var options = new JsonSerializerOptions { Converters = { (JsonConverter)converter } };
            string json = JsonSerializer.Serialize(cat, animalType, options);

            Assert.That(json, Does.Contain("\"type\":\"cat\""));
        }

        [Test]
        public void Deserialize_CatDiscriminator_ReturnsCat()
        {
            (Type animalType, _, object converter) = CompileAndLoad();

            var options = new JsonSerializerOptions { Converters = { (JsonConverter)converter } };
            object? result = JsonSerializer.Deserialize("{\"type\":\"cat\",\"Lives\":9,\"Age\":3}", animalType, options);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetType().Name, Is.EqualTo("Cat"));
        }

        [Test]
        public void Deserialize_UnknownDiscriminator_FallsBackToBase()
        {
            (Type animalType, _, object converter) = CompileAndLoad();

            var options = new JsonSerializerOptions { Converters = { (JsonConverter)converter } };
            object? result = JsonSerializer.Deserialize("{\"type\":\"fish\",\"Age\":3}", animalType, options);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.GetType().Name, Is.EqualTo("Animal"));
        }
    }
}
