using System;
using System.Text.Json;
using System.Threading.Tasks;
using NewApi;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class BaseIsAnInterfaceTests
    {
        [JsonSubTypeConverter(typeof(JsonSubtypes<IAnimal>), "Sound")]
        [KnownSubType(typeof(Dog), "Bark")]
        [KnownSubType(typeof(Cat), "Meow")]
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
            var animal = JsonSerializer.Deserialize<IAnimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}");
            Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
        }

        [Test]
        public void ConcurrentThreadTest()
        {
            Action test = () =>
            {
                var animal = JsonSerializer.Deserialize<IAnimal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
            };

            Parallel.For(0, 100, index => test());
        }

        [Test]
        public void UnknownMappingFails()
        {
            var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<IAnimal>("{\"Sound\":\"Scream\"}"));
            Assert.AreEqual("Could not create an instance of type JsonSubTypes.Tests.BaseIsAnInterfaceTests+IAnimal. Type is an interface or abstract class and cannot be instantiated. Position: 0.", exception.Message);
        }
    }
}
