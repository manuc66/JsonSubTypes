using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests.JsonPath
{
    class Nested: Main
    {
        string otherproperty { get; set; }
    }

    [JsonConverter(typeof(JsonSubtypes))]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(DottedSub), "nested.property")]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(Nested), "nested.otherproperty")]

    class Main
    {

    }

    class Sub : Main
    {
        Nested nested { get; set; }
    }

    class DottedSub : Main
    {
        [JsonProperty("nested.property")]
        string property { get; set; }
    }


    class NestedClass
    {

    }

    [JsonConverter(typeof(JsonSubtypes), "nested.property")]
    [JsonSubtypes.KnownSubType(typeof(SubDiscriminator), "SubNestedClass")]
    [JsonSubtypes.KnownSubType(typeof(OtherDiscriminator), "OtherNestedClass")]
    class MainDiscriminator
    {
        public NestedClass nested;

    }

    class SubNestedClass: NestedClass
    {
        string property = "SubNestedClass"; 
    }

    class OtherNestedClass: NestedClass
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


    [JsonConverter(typeof(JsonSubtypes), "dotted.property")]
    [JsonSubtypes.KnownSubType(typeof(SubDottedDiscriminator), "SubNestedClass")]
    class MainDottedDiscriminator
    {
        [JsonProperty("dotted.property")]
        string property { get; set; }
    }

    class SubDottedDiscriminator: MainDottedDiscriminator
    {
        
    }

    [TestFixture]
    public class JsonPathFallbackTests
    {
        [Test]
        public void CheckDottedProperty()
        {
            string json = "{\"nested.property\": \"str\"}";
            var result = JsonConvert.DeserializeObject<Main>(json);
            Assert.IsInstanceOf<DottedSub>(result);
        }

        [Test]
        public void CheckNestedProperty()
        {
            string json = "{nested: { otherproperty: \"abc\" } }";
            var result = JsonConvert.DeserializeObject<Main>(json);
            Assert.IsInstanceOf<Nested>(result);
        }

        [Test]
        public void CheckNestedDiscriminator()
        {
            string json = "{nested: { property: \"SubNestedClass\" } }";
            var result = JsonConvert.DeserializeObject<MainDiscriminator>(json);
            Assert.IsInstanceOf<SubDiscriminator>(result);
        }

        [Test]
        public void CheckDottedDiscriminator()
        {
            string json = "{\"dotted.property\": \"SubNestedClass\"}";
            var result = JsonConvert.DeserializeObject<MainDottedDiscriminator>(json);
            Assert.IsInstanceOf<SubDottedDiscriminator>(result);
        }
    }
}
