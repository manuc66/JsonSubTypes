using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    [TestFixture]
    public class DemoCustomSubclassMappingTests
    {
        [JsonConverter(typeof(JsonSubtypes), "Sound")]
        [JsonSubtypes.KnownSubType(typeof(Dog), "Bark")]
        [JsonSubtypes.KnownSubType(typeof(Cat), "Meow")]
        public class Animal
        {
            public virtual string Sound { get; }
            public string Color { get; set; }
        }

        public class Dog : Animal
        {
            public override string Sound { get; } = "Bark";
            public string Breed { get; set; }
        }

        public class Cat : Animal
        {
            public override string Sound { get; } = "Meow";
            public bool Declawed { get; set; }
        }

        [Test]
        public void Demo()
        {
            var animal = JsonConvert.DeserializeObject<Animal>("{\"Sound\":\"Bark\",\"Breed\":\"Jack Russell Terrier\"}");
            Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);

            animal = JsonConvert.DeserializeObject<Animal>("{\"Sound\":\"Meow\",\"Declawed\":\"true\"}");
            Assert.AreEqual(true, (animal as Cat)?.Declawed);
        }
    }
}
