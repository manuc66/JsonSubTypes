using System;
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
                Assert.Throws<JsonSerializationException>(() =>
                    JsonConvert.DeserializeObject<IAnimal>("{\"Kind\":8,\"Breed\":\"Jack Russell Terrier\"}")
                );
            }
        }
    }
}
