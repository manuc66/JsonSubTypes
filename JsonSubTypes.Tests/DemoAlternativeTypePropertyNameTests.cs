using Newtonsoft.Json;
using NUnit.Framework;

namespace JsonSubTypes.Tests
{
    namespace FullyQualifiedName
    {
        [JsonConverter(typeof(JsonSubtypes), "ClassName")]
        public class Animal
        {
            public virtual string ClassName { get; }
            public string Color { get; set; }
        }

        public class Dog : Animal
        {
            public override string ClassName { get; } = typeof(Dog).FullName;
            public string Breed { get; set; }
        }

        public class Cat : Animal
        {
            public override string ClassName { get; } = typeof(Cat).FullName;
            public bool Declawed { get; set; }
        }

        [TestFixture]
        public class DemoAlternativeTypePropertyWithFullNameTests
        {
            [Test]
            public void Demo()
            {
                var animal =
                    JsonConvert.DeserializeObject<Animal>(
                        "{\"ClassName\":\"JsonSubTypes.Tests.FullyQualifiedName.Dog\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
            }
        }
    }

    namespace SimplyQualifiedName
    {
        [JsonConverter(typeof(JsonSubtypes), "ClassName")]
        public class Animal
        {
            public virtual string ClassName { get; }
            public string Color { get; set; }
        }

        public class Dog : Animal
        {
            public override string ClassName { get; } = typeof(Dog).Name;
            public string Breed { get; set; }
        }

        public class Cat : Animal
        {
            public override string ClassName { get; } = typeof(Cat).Name;
            public bool Declawed { get; set; }
        }

        [TestFixture]
        public class DemoAlternativeTypePropertyNameTests
        {
            [Test]
            public void Demo()
            {
                var animal =
                    JsonConvert.DeserializeObject<Animal>(
                        "{\"ClassName\":\"Dog\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
            }

            [Test]
            public void DemoCaseInsensitive()
            {
                var animal =
                    JsonConvert.DeserializeObject<Animal>(
                        "{\"ClassName\":\"dog\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
            }

            [Test]
            public void DemoBaseWhenNull()
            {
                var animal =
                    JsonConvert.DeserializeObject<Animal>(
                        "{\"ClassName\": null,\"Color\":\"blue\"}");

                Assert.AreEqual(typeof(Animal), animal.GetType());
                Assert.AreEqual("blue", animal.Color);
            }

            [Test]
            public void ArbitraryConstructorShouldNotBeCalled()
            {
                Animal deserializeObject = null;
                try
                {
                    deserializeObject = JsonConvert.DeserializeObject<Animal>(@"{""ClassName"": ""Faulty""}");
                }
                catch { }

                Assert.IsFalse(Faulty.ctorCalled);
            }

        }

        public class Faulty
        {
            public static bool ctorCalled = false;
            public Faulty()
            {
                ctorCalled = true;
            }
        }
    }

    namespace SimplyQualifiedNameNestedType
    {
        [TestFixture]
        public class DemoAlternativeTypePropertyNameTests
        {
            [JsonConverter(typeof(JsonSubtypes), "Kind")]
            public interface IAnimal
            {
                string Kind { get; }
            }

            public class Dog : IAnimal
            {
                public string Kind { get; } = "Dog";
                public string Breed { get; set; }
            }

            [Test]
            public void Demo()
            {
                var animal =
                    JsonConvert.DeserializeObject<IAnimal>(
                        "{\"Kind\":\"Dog\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
            }

            [Test]
            public void DemoCaseInsensitive()
            {
                var animal =
                    JsonConvert.DeserializeObject<IAnimal>(
                        "{\"Kind\":\"dog\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
            }

            [Test]
            public void WhenNoMappingPossible()
            {
                var exception = Assert.Throws<JsonSerializationException>(() =>
                    JsonConvert.DeserializeObject<IAnimal>("{\"Kind\":8,\"Breed\":\"Jack Russell Terrier\"}")
                );
                Assert.AreEqual("Could not create an instance of type JsonSubTypes.Tests.SimplyQualifiedNameNestedType.DemoAlternativeTypePropertyNameTests+IAnimal. Type is an interface or abstract class and cannot be instantiated. Path 'Kind', line 1, position 8.", exception.Message);
            }
        }
    }

    namespace FallBackSubType
    {
        [TestFixture]
        public class DemoAlternativeTypePropertyNameTests
        {
            [JsonConverter(typeof(JsonSubtypes), "Kind")]
            [JsonSubtypes.FallBackSubType(typeof(UnknownAnimal))]
            public interface IAnimal
            {
                string Kind { get; }
            }

            public class UnknownAnimal : IAnimal
            {
                public string Kind { get; set; }
            }

            public class Dog : IAnimal
            {
                public string Kind { get; } = "Dog";
                public string Breed { get; set; }
            }

            [Test]
            public void Demo()
            {
                var animal =
                    JsonConvert.DeserializeObject<IAnimal>(
                        "{\"Kind\":\"Dog\",\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
            }

            [Test]
            public void WhenNoMappingPossible()
            {
                var animal = JsonConvert.DeserializeObject<IAnimal>("{\"Kind\":\"Octopus\",\"Specie\":\"Octopus tetricus\"}");

                Assert.AreEqual("Octopus", ((UnknownAnimal)animal).Kind);
            }

            [Test]
            public void WhenNoMappingPossibleAndNull()
            {
                var animal = JsonConvert.DeserializeObject<IAnimal>("{\"Kind\": null,\"Specie\":\"Octopus tetricus\"}");

                Assert.AreEqual(null, ((UnknownAnimal)animal).Kind);
            }
        }
    }

    namespace FallBackSubTypeWithNullMapping
    {
        [TestFixture]
        public class DemoAlternativeTypePropertyNameTests
        {
            [JsonConverter(typeof(JsonSubtypes), "Kind")]
            [JsonSubtypes.KnownSubType(typeof(Dog), null)]
            [JsonSubtypes.FallBackSubType(typeof(UnknownAnimal))]
            public interface IAnimal
            {
                string Kind { get; }
            }

            public class UnknownAnimal : IAnimal
            {
                public string Kind { get; set; }
            }

            public class Dog : IAnimal
            {
                public string Kind { get; } = null;
                public string Breed { get; set; }
            }

            [Test]
            public void Demo()
            {
                var animal =
                    JsonConvert.DeserializeObject<IAnimal>(
                        "{\"Kind\":null,\"Breed\":\"Jack Russell Terrier\"}");
                Assert.AreEqual("Jack Russell Terrier", (animal as Dog)?.Breed);
            }

            [Test]
            public void WhenNoMappingPossible()
            {
                var animal = JsonConvert.DeserializeObject<IAnimal>("{\"Kind\":\"Octopus\",\"Specie\":\"Octopus tetricus\"}");

                Assert.AreEqual("Octopus", (animal as UnknownAnimal)?.Kind);
            }
        }
    }
}

