using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class DivergenceTests
    {
        public class CustomAnimalConverter : JsonSubtypes<ClassWithCustomConverter>
        {
            public CustomAnimalConverter() : base("Kind") { }
        }

        [JsonSubTypeConverter(typeof(CustomAnimalConverter))]
        public class ClassWithCustomConverter
        {
            public string Kind { get; set; }
        }

        [Test]
        public void DerivedNonGenericConverterTypeInAttributeDoesNotThrowIndexOutOfRange()
        {
            var json = "{\"Kind\":\"ClassWithCustomConverter\"}";
            Assert.DoesNotThrow(() => JsonSerializer.Deserialize<ClassWithCustomConverter>(json));
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<BaseWithCustomPropConverter>), "Kind")]
        [KnownSubType(typeof(DerivedWithCustomPropConverter), "Derived")]
        public class BaseWithCustomPropConverter
        {
            public string Kind { get; set; }

            [JsonConverter(typeof(UpperStringConverter))]
            public string Label { get; set; }
        }

        public class DerivedWithCustomPropConverter : BaseWithCustomPropConverter
        {
            public int Value { get; set; }
        }

        public class UpperStringConverter : JsonConverter<string>
        {
            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return reader.GetString()?.ToUpperInvariant();
            }

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value?.ToUpperInvariant());
            }
        }

        [Test]
        public void PropertyLevelJsonConverterHonoredOnDerivedSubtype()
        {
            var derived = new DerivedWithCustomPropConverter { Kind = "Derived", Label = "hello", Value = 42 };
            var json = JsonSerializer.Serialize<BaseWithCustomPropConverter>(derived);

            Assert.That(json, Does.Contain("\"Label\":\"HELLO\""));

            var deserialized = JsonSerializer.Deserialize<BaseWithCustomPropConverter>("{\"Kind\":\"Derived\",\"Label\":\"world\",\"Value\":10}");
            Assert.IsInstanceOf<DerivedWithCustomPropConverter>(deserialized);
            Assert.That(deserialized.Label, Is.EqualTo("WORLD"));
        }

        [JsonSubTypeConverter(typeof(JsonSubtypes<AnimalWithConditionalIgnore>), "Kind")]
        [KnownSubType(typeof(DogWithConditionalIgnore), "Dog")]
        public class AnimalWithConditionalIgnore
        {
            public string Kind { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Nickname { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Missing { get; set; }
        }

        public class DogWithConditionalIgnore : AnimalWithConditionalIgnore
        {
        }

        [Test]
        public void FallbackWriteHonorsJsonIgnoreCondition()
        {
            var json = JsonSerializer.Serialize<AnimalWithConditionalIgnore>(
                new AnimalWithConditionalIgnore { Kind = "Animal", Nickname = "Buddy" });

            Assert.That(json, Does.Contain("\"Nickname\":\"Buddy\""));
            Assert.That(json, Does.Not.Contain("\"Missing\""));
        }

        [Test]
        public void FallbackReadHonorsWhenWritingNullIgnoreAsReadable()
        {
            var animal = JsonSerializer.Deserialize<AnimalWithConditionalIgnore>(
                "{\"Kind\":\"Animal\",\"Nickname\":\"Buddy\",\"Missing\":null}");

            Assert.That(animal.Nickname, Is.EqualTo("Buddy"));
        }
    }
}
