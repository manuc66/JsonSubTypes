using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    // The convenience JsonSubTypeConverterAttribute constructors close the generic
    // JsonSubtypes<T> converter over the annotated type, so the base type is not
    // repeated on the attribute: [JsonSubTypeConverter("Kind")] instead of
    // [JsonSubTypeConverter(typeof(JsonSubtypes<Animal>), "Kind")].
    [TestFixture]
    public class ConvenienceAttributeTests
    {
        [JsonSubTypeConverter("Kind")]
        [KnownSubType(typeof(ConvDog), "Dog")]
        [KnownSubType(typeof(ConvCat), "Cat")]
        public class ConvAnimal
        {
            public string Kind { get; set; }
            public int Age { get; set; }
        }

        public class ConvDog : ConvAnimal
        {
            public string Breed { get; set; }
        }

        public class ConvCat : ConvAnimal
        {
            public bool Declawed { get; set; }
        }

        [JsonSubTypeConverter]
        [KnownSubTypeWithProperty(typeof(PresEmployee), "JobTitle")]
        public class PresencePerson
        {
            public string FirstName { get; set; }
        }

        public class PresEmployee : PresencePerson
        {
            public string JobTitle { get; set; }
        }

        [JsonSubTypeConverter("Type")]
        [KnownSubType(typeof(ImplExpression), "impl")]
        public interface IExpression
        {
            string Type { get; }
        }

        public class ImplExpression : IExpression
        {
            public string Type { get; } = "impl";
            public int Value { get; set; }
        }

        [Test]
        public void ValueBased_WritesDiscriminatorAndRoundTrips()
        {
            string json = JsonSerializer.Serialize<ConvAnimal>(new ConvDog { Kind = "Dog", Age = 3, Breed = "Rex" });

            StringAssert.Contains("\"Kind\":\"Dog\"", json);
            StringAssert.Contains("\"Breed\":\"Rex\"", json);
            Assert.AreEqual(1, System.Text.RegularExpressions.Regex.Matches(json, "\"Kind\"").Count);

            var back = JsonSerializer.Deserialize<ConvAnimal>(json);
            Assert.IsInstanceOf<ConvDog>(back);
            Assert.AreEqual("Rex", (back as ConvDog)?.Breed);
        }

        [Test]
        public void ValueBased_DiscriminatorNotANativeProperty_IsInjected()
        {
            string json = JsonSerializer.Serialize<ConvAnimal>(new ConvDog { Age = 3, Breed = "Rex" });

            StringAssert.Contains("\"Kind\":\"Dog\"", json);
        }

        [Test]
        public void PropertyPresence_RoundTrips()
        {
            string json = JsonSerializer.Serialize<PresencePerson>(new PresEmployee { FirstName = "Ann", JobTitle = "Dev" });

            using JsonDocument doc = JsonDocument.Parse(json);
            Assert.AreEqual(2, doc.RootElement.EnumerateObject().Count());
            CollectionAssert.AreEquivalent(
                new[] { "FirstName", "JobTitle" }, doc.RootElement.EnumerateObject().Select(p => p.Name).ToList());

            var back = JsonSerializer.Deserialize<PresencePerson>(json);
            Assert.IsInstanceOf<PresEmployee>(back);
            Assert.AreEqual("Dev", (back as PresEmployee)?.JobTitle);
        }

        [Test]
        public void InterfaceBase_RoundTrips()
        {
            string json = JsonSerializer.Serialize<IExpression>(new ImplExpression { Value = 7 });

            var back = JsonSerializer.Deserialize<IExpression>(json);
            Assert.IsInstanceOf<ImplExpression>(back);
            Assert.AreEqual(7, (back as ImplExpression)?.Value);
        }
    }
}
