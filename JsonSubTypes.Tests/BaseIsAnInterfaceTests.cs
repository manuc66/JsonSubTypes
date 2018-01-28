using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
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

        [Test]
        public void Test()
        {
            var annimal = JsonConvert.DeserializeObject<IAnnimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}");
            Assert.AreEqual("Jack Russell Terrier", (annimal as Dog)?.Breed);
        }

        [Test]
        public void ConcurrentThreadTest()
        {
            Action test = () =>
            {
                var annimal = JsonConvert.DeserializeObject<IAnnimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (annimal as Dog)?.Breed);
            };

            Parallel.For(0, 100, index => test());
        }

        [Test]
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
