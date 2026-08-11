#nullable enable
using System.Text.Json;
using JsonSubTypes.Aot.Generated;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Aot.Tests
{
    [TestFixture]
    public class GeneratedValueModeTests
    {
        private static JsonSerializerOptions Options()
        {
            return new JsonSerializerOptions
            {
                Converters = { JsonSubTypesAotConverters.Animal }
            };
        }

        [Test]
        public void Serialize_Subtype_WritesDiscriminator()
        {
            string json = JsonSerializer.Serialize<Animal>(new Cat { Age = 11, Lives = 6 }, Options());

            Assert.AreEqual("{\"type\":\"cat\",\"Lives\":6,\"Age\":11}", json);
        }

        [Test]
        public void Deserialize_ReturnsCorrectSubtype()
        {
            var result = JsonSerializer.Deserialize<Animal>("{\"type\":\"cat\",\"Lives\":6,\"Age\":11}", Options());

            Assert.IsInstanceOf<Cat>(result);
        }

        [Test]
        public void RoundTrip_PreservesSubtype()
        {
            var options = Options();
            var original = new Dog { Age = 4, CanHunt = true };

            string json = JsonSerializer.Serialize<Animal>(original, options);
            var back = JsonSerializer.Deserialize<Animal>(json, options);

            Assert.IsInstanceOf<Dog>(back);
        }

        [Test]
        public void Deserialize_IntDiscriminator_ReturnsCorrectSubtype()
        {
            var result = JsonSerializer.Deserialize<Animal>("{\"type\":2,\"CanHunt\":true,\"Age\":4}", Options());

            Assert.IsInstanceOf<Dog>(result);
        }

        [Test]
        public void Deserialize_UnknownDiscriminator_FallsBackToBase()
        {
            var result = JsonSerializer.Deserialize<Animal>("{\"type\":\"fish\",\"Age\":1}", Options());

            Assert.IsInstanceOf<Animal>(result);
        }

        [Test]
        public void Serialize_BaseInstance_WithoutMapping_WritesPlainObject()
        {
            // matches the attribute-based runtime converter: an unregistered base is serialized
            // without a discriminator instead of throwing
            var options = Options();

            string json = JsonSerializer.Serialize<Animal>(new Animal { Age = 1 }, options);

            Assert.AreEqual("{\"Age\":1}", json);
        }

        [Test]
        public void Serialize_Collection_WritesDiscriminators()
        {
            var options = Options();
            var animals = new Animal[] { new Cat { Age = 1, Lives = 9 }, new Dog { Age = 2, CanHunt = false } };

            string json = JsonSerializer.Serialize(animals, options);

            Assert.AreEqual(
                "[{\"type\":\"cat\",\"Lives\":9,\"Age\":1},{\"type\":2,\"CanHunt\":false,\"Age\":2}]",
                json);
        }
    }

    [TestFixture]
    public class GeneratedPresenceModeTests
    {
        private static JsonSerializerOptions Options()
        {
            return new JsonSerializerOptions
            {
                Converters = { JsonSubTypesAotConverters.Person }
            };
        }

        [Test]
        public void Deserialize_ByPropertyPresence_ReturnsCorrectSubtype()
        {
            var artist = JsonSerializer.Deserialize<Person>("{\"Skill\":\"Painter\",\"FirstName\":\"A\"}", Options());
            var employee = JsonSerializer.Deserialize<Person>("{\"JobTitle\":\"Dev\",\"FirstName\":\"B\"}", Options());

            Assert.IsInstanceOf<Artist>(artist);
            Assert.IsInstanceOf<Employee>(employee);
        }

        [Test]
        public void Deserialize_NoMatchingProperty_FallsBackToBase()
        {
            var result = JsonSerializer.Deserialize<Person>("{\"FirstName\":\"C\"}", Options());

            Assert.IsInstanceOf<Person>(result);
        }

        [Test]
        public void Serialize_DoesNotWriteDiscriminator()
        {
            string json = JsonSerializer.Serialize<Person>(new Artist { Skill = "Painter", FirstName = "A" }, Options());

            Assert.AreEqual("{\"Skill\":\"Painter\",\"FirstName\":\"A\"}", json);
        }
    }

    // ---- domain types ----

    [JsonSubTypesAotConverter("type")]
    [KnownSubType(typeof(Cat), "cat")]
    [KnownSubType(typeof(Dog), 2)]
    public class Animal
    {
        public int Age { get; set; }
    }

    public class Cat : Animal
    {
        public int Lives { get; set; }
    }

    public class Dog : Animal
    {
        public bool CanHunt { get; set; }
    }

    [JsonSubTypesAotConverter]
    [KnownSubTypeWithProperty(typeof(Employee), "JobTitle")]
    [KnownSubTypeWithProperty(typeof(Artist), "Skill")]
    public class Person
    {
        public string? FirstName { get; set; }
    }

    public class Employee : Person
    {
        public string? JobTitle { get; set; }
    }

    public class Artist : Person
    {
        public string? Skill { get; set; }
    }
}
