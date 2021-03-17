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
        public interface IAnimal
        {
            string Sound { get; }
        }

        public class Dog : IAnimal
        {
            public string Sound { get; } = "Bark";
            public string Breed { get; set; }
        }

        public class Cat : IAnimal
        {
            public string Sound { get; } = "Meow";
            public bool Declawed { get; set; }
        }

        [Test]
        public void Test()
        {
            var animal = JsonConvert.DeserializeObject<IAnimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}");
            Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
        }

        [Test]
        public void ConcurrentThreadTest()
        {
            Action test = () =>
            {
                var animal = JsonConvert.DeserializeObject<IAnimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
            };

            Parallel.For(0, 100, index => test());
        }

        [Test]
        public void UnknownMappingFails()
        {
            var exception = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<IAnimal>("{\"Sound\":\"Scream\"}"));
            Assert.AreEqual("No instantiable subtype has been found for: JsonSubTypes.Tests.BaseIsAnInterfaceTests+IAnimal. Path '', line 1, position 1.", exception.Message);
        }
    }
}
