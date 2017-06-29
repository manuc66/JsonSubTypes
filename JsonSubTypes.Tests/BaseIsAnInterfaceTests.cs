using Newtonsoft.Json;
using Xunit;

namespace JsonSubTypes.Tests
{
    public class BaseIsAnInterfaceTests
    {
        [JsonConverter(typeof(JsonSubtypes), "Sound")]
        [JsonSubtypes.KnownSubType(typeof(Dog), "Bark")]
        [JsonSubtypes.KnownSubType(typeof(Cat), "Meow")]
        public interface IAnnimal
        {
            string Sound { get; }
        }

        public class Dog : IAnnimal
        {
            public string Sound { get; } = "Bark";
            public string Breed { get; set; }
        }

        public class Cat : IAnnimal
        {
            public string Sound { get; } = "Meow";
            public bool Declawed { get; set; }
        }

        [Fact]
        public void Test()
        {
            var annimal = JsonConvert.DeserializeObject<IAnnimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}");
            Assert.Equal("Jack Russell Terrier", (annimal as Dog)?.Breed);
        }

        [Fact]
        public void UnknownMappingFails()
        {
            try
            {
                JsonConvert.DeserializeObject<IAnnimal>("{\"Sound\":\"Scream\"}");
                Assert.True(false);
            }
            catch (JsonSerializationException)
            {
                
            }
        }
    }
}
