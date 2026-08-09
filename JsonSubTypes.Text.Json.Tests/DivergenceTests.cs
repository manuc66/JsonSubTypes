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

        [JsonSubTypeConverter(typeof(JsonSubtypes<BaseWithConditionalIgnores>), "Kind")]
        [KnownSubType(typeof(DerivedWithConditionalIgnores), "Derived")]
        public class BaseWithConditionalIgnores
        {
            public string Kind { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string Optional { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public int Count { get; set; }

            [JsonIgnore]
            public string Secret { get; set; }
        }

        public class DerivedWithConditionalIgnores : BaseWithConditionalIgnores
        {
            public bool Barks { get; set; }
        }

        [Test]
        public void BaseTypedWriteHonorsJsonIgnoreCondition()
        {
            var json = JsonSerializer.Serialize<BaseWithConditionalIgnores>(
                new BaseWithConditionalIgnores { Kind = "Base", Optional = "set", Count = 5, Secret = "hidden" });

            Assert.That(json, Does.Contain("\"Optional\":\"set\""));
            Assert.That(json, Does.Contain("\"Count\":5"));
            Assert.That(json, Does.Not.Contain("Secret"));
        }

        [Test]
        public void BaseTypedWriteDropsNullAndDefaultValuesWithJsonIgnoreCondition()
        {
            var json = JsonSerializer.Serialize<BaseWithConditionalIgnores>(
                new BaseWithConditionalIgnores { Kind = "Base", Optional = null, Count = 0, Secret = "hidden" });

            Assert.That(json, Does.Not.Contain("Optional"));
            Assert.That(json, Does.Not.Contain("Count"));
            Assert.That(json, Does.Not.Contain("Secret"));
        }

        [Test]
        public void BaseTypedReadHonorsJsonIgnoreCondition()
        {
            var back = JsonSerializer.Deserialize<BaseWithConditionalIgnores>(
                "{\"Kind\":\"Unknown\",\"Optional\":\"hello\",\"Count\":7,\"Secret\":\"nope\"}");

            Assert.IsInstanceOf<BaseWithConditionalIgnores>(back);
            Assert.That(back.Optional, Is.EqualTo("hello"));
            Assert.That(back.Count, Is.EqualTo(7));
            Assert.That(back.Secret, Is.Null);
        }

        [Test]
        public void DerivedSubtypeRoundTripKeepsConditionalIgnores()
        {
            var derived = new DerivedWithConditionalIgnores { Kind = "Derived", Optional = null, Count = 0, Secret = "hidden", Barks = true };
            var json = JsonSerializer.Serialize<BaseWithConditionalIgnores>(derived);

            Assert.That(json, Does.Not.Contain("Optional"));
            Assert.That(json, Does.Not.Contain("Count"));
            Assert.That(json, Does.Not.Contain("Secret"));
            Assert.That(json, Does.Contain("\"Barks\":true"));

            var back = JsonSerializer.Deserialize<BaseWithConditionalIgnores>(json);
            Assert.IsInstanceOf<DerivedWithConditionalIgnores>(back);
            Assert.That(((DerivedWithConditionalIgnores)back).Barks, Is.True);
        }
    }
}
