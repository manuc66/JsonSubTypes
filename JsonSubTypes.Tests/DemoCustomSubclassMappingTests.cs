using Newtonsoft.Json;
using Xunit;

namespace JsonSubTypes.Tests
{

    public class DemoCustomSubclassMappingTests
    {
        [JsonConverter(typeof(JsonSubtypes), "Sound")]
        [JsonSubtypes.KnownSubType(typeof(Dog), "Bark")]
        [JsonSubtypes.KnownSubType(typeof(Cat), "Meow")]
        public class Annimal
        {
            public virtual string Sound { get; }
            public string Color { get; set; }
        }

        public class Dog : Annimal
        {
            public override string Sound { get; } = "Bark";
            public string Breed { get; set; }
        }

        public class Cat : Annimal
        {
            public override string Sound { get; } = "Meow";
            public bool Declawed { get; set; }
        }

        [Fact]
        public void Demo()
        {
            var annimal = JsonConvert.DeserializeObject<Annimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}");
            Assert.Equal("Jack Russell Terrier", (annimal as Dog)?.Breed);
        }
    }
}
