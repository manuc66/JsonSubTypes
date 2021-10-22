using System.Text.Json;
using System.Text.Json.Serialization;
using JsonSubTypes.Text.Json;
using NewApi;
using NUnit.Framework;

namespace JsonSubTypes.Tests.JsonPath
{
    class Nested : Main
    {
        string otherproperty { get; set; }
    }

    [JsonSubTypeConverter(typeof(JsonSubtypes<Main>))]
    [KnownSubTypeWithProperty(typeof(DottedSub), "nested.property")]
    [KnownSubTypeWithProperty(typeof(Nested), "nested.otherproperty")]

    class Main
    {

    }

    class Sub : Main
    {
        Nested nested { get; set; }
    }

    class DottedSub : Main
    {
        [JsonPropertyName("nested.property")]
        string property { get; set; }
    }


    class NestedClass
    {

    }

    [JsonSubTypeConverter(typeof(JsonSubtypes<MainDiscriminator>), "nested.property")]
    [KnownSubType(typeof(SubDiscriminator), "SubNestedClass")]
    [KnownSubType(typeof(OtherDiscriminator), "OtherNestedClass")]
    class MainDiscriminator
    {
        public NestedClass nested;

    }

    class SubNestedClass : NestedClass
    {
        string property = "SubNestedClass";
    }

    class OtherNestedClass : NestedClass
    {
        string property = "OtherNestedClass";
    }

    class SubDiscriminator : MainDiscriminator
    {
        new SubNestedClass nested { get; set; }
    }

    class OtherDiscriminator : MainDiscriminator
    {
        new OtherNestedClass nested { get; set; }
    }


    [JsonSubTypeConverter(typeof(JsonSubtypes<MainDottedDiscriminator>), "dotted.property")]
    [KnownSubType(typeof(SubDottedDiscriminator), "SubNestedClass")]
    class MainDottedDiscriminator
    {
        [JsonPropertyName("dotted.property")]
        string property { get; set; }
    }

    class SubDottedDiscriminator : MainDottedDiscriminator
    {

    }

    [TestFixture]
    public class JsonPathFallbackTests
    {
        [Test]
        public void CheckDottedProperty()
        {
            string json = "{\"nested.property\": \"str\"}";
            var result = JsonSerializer.Deserialize<Main>(json);
            Assert.IsInstanceOf<DottedSub>(result);
        }

        [Test]
        public void CheckNestedProperty()
        {
            string json = "{nested: { otherproperty: \"abc\" } }";
            var result = JsonSerializer.Deserialize<Main>(json);
            Assert.IsInstanceOf<Nested>(result);
        }

        [Test]
        public void CheckNestedDiscriminator()
        {
            string json = "{nested: { property: \"SubNestedClass\" } }";
            var result = JsonSerializer.Deserialize<MainDiscriminator>(json);
            Assert.IsInstanceOf<SubDiscriminator>(result);
        }

        [Test]
        public void CheckDottedDiscriminator()
        {
            string json = "{\"dotted.property\": \"SubNestedClass\"}";
            var result = JsonSerializer.Deserialize<MainDottedDiscriminator>(json);
            Assert.IsInstanceOf<SubDottedDiscriminator>(result);
        }
    }
}
