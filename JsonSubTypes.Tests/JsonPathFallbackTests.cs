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
        public void CheckJsonPathFallback()
        {
            string json = "{nested: { otherproperty: \"abc\" } }";
            var result = JsonConvert.DeserializeObject<Main>(json);
            Assert.IsInstanceOf<Nested>(result);
        }
    }
}
