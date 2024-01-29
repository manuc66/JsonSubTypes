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
            Assert.AreEqual("Could not create an instance of type JsonSubTypes.Tests.BaseIsAnInterfaceTests+IAnimal. Type is an interface or abstract class and cannot be instantiated. Path 'Sound', line 1, position 9.", exception.Message);
        }
    }

    [TestFixture]
    public class KnownBaseType_BaseIsAnInterfaceTests
    {
        [JsonConverter(typeof(JsonSubtypes), "Sound")]
        public interface IAnimal
        {
            string Sound { get; }
        }

        [JsonSubtypes.KnownBaseType(typeof(IAnimal), "Bark")]
        public class Dog : IAnimal
        {
            public string Sound { get; } = "Bark";
            public string Breed { get; set; }
        }

        [JsonSubtypes.KnownBaseType(typeof(IAnimal), "Meow")]
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
            Assert.AreEqual("Could not create an instance of type JsonSubTypes.Tests.KnownBaseType_BaseIsAnInterfaceTests+IAnimal. Type is an interface or abstract class and cannot be instantiated. Path 'Sound', line 1, position 9.", exception.Message);
        }
    }
}
